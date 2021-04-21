﻿using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using Jither.Midi.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Messages
{
    public class ImuseSysexParser : ISysexParser
    {
        public int ManufacturerId => 0x7d;

        public SysexMessage Parse(byte[] data)
        {
            return data[1] switch
            {
                0x00 => data.Length == 4 ? new ImuseV2Marker(data) : new ImuseAllocPart(data),
                0x01 => data.Length == 11 ? new ImuseV2HookJump(data) : new ImuseDeallocPart(data),
                0x02 => new ImuseDeallocAllParts(data),
                0x10 => new ImuseActiveSetup(data),
                0x11 => new ImuseStoredSetup(data),
                0x12 => new ImuseSetupBank(data),
                0x20 => new ImuseSystemParam(data),
                0x21 => new ImuseSetupParam(data),
                0x30 => new ImuseHookJump(data),
                0x31 => new ImuseHookTranspose(data),
                0x32 => new ImuseHookPartEnable(data),
                0x33 => new ImuseHookPartVol(data),
                0x34 => new ImuseHookPartPgmch(data),
                0x35 => new ImuseHookPartTranspose(data),
                0x40 => new ImuseMarker(data),
                0x50 => new ImuseSetLoop(data),
                0x51 => new ImuseClearLoop(data),
                0x60 => new ImuseLoadSetup(data),
                _ => new ImuseUnknown(data)
            };
        }
    }

    public abstract class ImuseMessage : SysexMessage
    {
        public override string Name => "imuse";
        public override string Parameters => $"{ImuseMessageName,-20} {Info}";
        protected abstract string Info { get; }
        protected abstract string ImuseMessageName { get; }

        public override string ToString() => $"{Name,-11} {Channel,2}  {Parameters}";

        public byte[] ImuseData { get; private set; }
        public ArraySegment<byte> ImuseByteData { get; }

        public int Channel { get; }

        protected virtual int ByteDataLength => 1;
        protected virtual bool HasChannel => true;

        protected ImuseMessage(byte[] data) : base(data)
        {
            // Skip manufacturer ID and type
            int dataIndex = 2;
            // The first few bytes after that are 7 bit data - length depends on message type
            ImuseByteData = new ArraySegment<byte>(data, dataIndex, ByteDataLength);

            // In v1, the first byte is the channel
            if (HasChannel)
            {
                if ((ImuseByteData[0] & 0xf0) != 0)
                {
                    throw new MidiMessageException($"iMuse message has channel high nibble set: {ImuseByteData[0]:x2} - full sysex: {data.ToHex()}");
                }
                Channel = ImuseByteData[0];
            }

            dataIndex += ByteDataLength;

            // The remainder is bytes with nibbles distributed into two bytes
            UnpackNibbles(data, dataIndex);
        }

        private void UnpackNibbles(byte[] data, int source)
        {
            int nibblesEnd = data.Length - 1;

            ImuseData = new byte[(data.Length - source) / 2];

            int dest = 0;

            while (source < nibblesEnd)
            {
                byte hi = data[source++];
                if ((hi & 0x80) > 0)
                {
                    throw new MidiMessageException($"iMuse message data has high bit set: {hi:x2} - full sysex: {data.ToHex()}");
                }
                byte unpacked = hi;
                if (source < nibblesEnd)
                {
                    byte lo = data[source++];
                    if ((lo & 0xf0) > 0)
                    {
                        throw new MidiMessageException($"iMuse message data has high bit set: {hi:x2} - full sysex {data.ToHex()}");
                    }
                    unpacked = (byte)(unpacked << 4 | (lo & 0xf));
                }
                ImuseData[dest++] = unpacked;
            }
            if (data[source] != 0xf7)
            {
                throw new MidiMessageException($"Expected end of sysex (f7), but found: {data[source]:x2} - full sysex: {data.ToHex()}");
            }
        }
    }

    public class ImuseUnknown : ImuseMessage
    {
        protected override string Info => Data.ToHex();
        protected override string ImuseMessageName => "unknown";

        public ImuseUnknown(byte[] data) : base(data)
        {
        }
    }
    public class ImuseAllocPart : ImuseMessage
    {
        public const int transposeLockedFlag = -128;

        protected override string ImuseMessageName => "alloc-part";
        protected override string Info => $"enabled: {Enabled,5}, reverb: {Reverb,5}, prioffs: {PriorityOffset,3}, vol: {Volume,3}, pan: {Pan,3}, trans: {(TransposeLocked ? "lock" : Transpose),4}, detune: {Detune,3}, pbr: {PitchBendRange,3}, pgm: {Program,3}";

        public bool Enabled { get; }
        public bool Reverb { get; }
        public int PriorityOffset { get; }
        public int Volume { get; }
        public int Pan { get; }
        public int Transpose { get; }
        public int Detune { get; }
        public int PitchBendRange { get; }
        public int Program { get; }
        public bool TransposeLocked => Transpose == transposeLockedFlag;

        public ImuseAllocPart(byte[] data) : base(data)
        {
            Enabled = (ImuseData[0] & 0x01) != 0;
            Reverb = (ImuseData[0] & 0x02) != 0;
            PriorityOffset = ImuseData[1];
            Volume = ImuseData[2];
            Pan = (sbyte)ImuseData[3];
            Transpose = (sbyte)ImuseData[4];
            Detune = (sbyte)ImuseData[5];
            PitchBendRange = ImuseData[6];
            Program = ImuseData[7];
        }
    }

    public class ImuseDeallocPart : ImuseMessage
    {
        protected override string ImuseMessageName => "dealloc-part";
        protected override string Info => "";

        public ImuseDeallocPart(byte[] data) : base(data)
        {
        }
    }

    public class ImuseDeallocAllParts : ImuseMessage
    {
        protected override string ImuseMessageName => "dealloc-all-parts";
        protected override string Info => "";

        public ImuseDeallocAllParts(byte[] data) : base(data)
        {
        }
    }

    public class ImuseActiveSetup : ImuseMessage
    {
        protected override string ImuseMessageName => "active-setup";
        protected override string Info => $"hw-id: {HardwareId,3}, setup: {Setup.ToHex()}";
        protected override int ByteDataLength => 2;

        public HardwareId HardwareId { get; }
        public byte[] Setup { get; }

        public ImuseActiveSetup(byte[] data) : base(data)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            Setup = ImuseData;
        }
    }

    public class ImuseStoredSetup : ImuseMessage
    {
        protected override string ImuseMessageName => "stored-setup";
        protected override string Info => $"hw-id: {HardwareId,3}, setup-number: {SetupNumber,3}, setup: {Setup.ToHex()}";
        protected override int ByteDataLength => 3;

        public HardwareId HardwareId { get; }
        public int SetupNumber { get; }
        public byte[] Setup { get; }

        public ImuseStoredSetup(byte[] data) : base(data)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            SetupNumber = ImuseByteData[2];
            Setup = ImuseData;
        }
    }

    public class ImuseSetupBank : ImuseMessage
    {
        protected override string ImuseMessageName => "setup-bank";
        protected override string Info => "";

        public ImuseSetupBank(byte[] data) : base(data)
        {
        }
    }

    public class ImuseSystemParam : ImuseMessage
    {
        protected override string ImuseMessageName => "system-param";
        protected override string Info => "";

        public ImuseSystemParam(byte[] data) : base(data)
        {
        }
    }

    public class ImuseSetupParam : ImuseMessage
    {
        protected override string ImuseMessageName => "setup-param";
        protected override string Info => $"hw-id: {HardwareId,3}, param number: {Number,5}, value: {Value,5}";
        protected override int ByteDataLength => 2;

        public HardwareId HardwareId { get; }
        public int Number { get; }
        public int Value { get; }
        public ImuseSetupParam(byte[] data) : base(data)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            Number = ImuseData[0] << 8 | ImuseData[1];
            Value = ImuseData[2] << 8 | ImuseData[3];
        }
    }

    public class ImuseHookJump : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-jump";
        protected override string Info => $"hook: {Hook,3}, chunk: {Chunk,5}, beat: {Beat,5}, tick: {Tick,5}";

        public int Hook { get; }
        public int Chunk { get; }
        public int Beat { get; }
        public int Tick { get; }

        public ImuseHookJump(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            // Yes, ImuseData is full 8-bit bytes (unpacked from nibbles), so << 8 is correct.
            Chunk = ImuseData[1] << 8 | ImuseData[2];
            Beat = ImuseData[3] << 8 | ImuseData[4];
            Tick = ImuseData[5] << 8 | ImuseData[6];
        }
    }

    public class ImuseV2HookJump : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-jump-v2";
        protected override string Info => $"hook: {Hook,3}, chunk: {Chunk,3}, measure: {Measure,5}, beat: {Beat,3}, tick: {Tick,3}, sustain: {Sustain}";
        protected override int ByteDataLength => 8;
        protected override bool HasChannel => false;

        public int Hook { get; }
        public int Chunk { get; }
        public int Measure { get; }
        public int Beat { get; }
        public int Tick { get; }
        public bool Sustain { get; }

        public ImuseV2HookJump(byte[] data) : base(data)
        {
            Hook = ImuseByteData[0];
            Chunk = ImuseByteData[1] - 1; // Chunk is 1-indexed rather than 0-indexed in v2. We keep it 0-indexed.
            Measure = ImuseByteData[2] << 7 | ImuseByteData[3]; // Measure is 1-indexed
            Beat = ImuseByteData[4];
            Tick = ImuseByteData[5] << 7 | ImuseByteData[6];

            if (ImuseByteData[7] > 1)
            {
                throw new MidiMessageException($"Unexpected sustain value in v2 hook-jump: {ImuseByteData[7]:x2}");
            }

            Sustain = ImuseByteData[7] == 1;
        }
    }

    public class ImuseHookTranspose : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-transpose";
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public int Hook { get; }
        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookTranspose(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = (sbyte)ImuseData[2];
        }
    }

    public class ImuseHookPartEnable : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-part-enable";
        protected override string Info => $"hook: {Hook,3}, state: {Enabled,3}";

        public int Hook { get; }
        public int Enabled { get; }

        public ImuseHookPartEnable(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Enabled = ImuseData[1];
        }
    }

    public class ImuseHookPartVol : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-part-vol";
        protected override string Info => $"hook: {Hook,3}, vol: {Volume,3}";

        public int Hook { get; }
        public int Volume { get; }

        public ImuseHookPartVol(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Volume = ImuseData[1];
        }
    }

    public class ImuseHookPartPgmch : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-part-pgmch";
        protected override string Info => $"hook: {Hook,3}, vol: {Program,3}";

        public int Hook { get; }
        public int Program { get; }

        public ImuseHookPartPgmch(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Program = ImuseData[1];
        }
    }

    public class ImuseHookPartTranspose : ImuseMessage
    {
        protected override string ImuseMessageName => "hook-part-transpose";
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public int Hook { get; }
        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookPartTranspose(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = (sbyte)ImuseData[2];
        }
    }

    public class ImuseMarker : ImuseMessage
    {
        protected override string ImuseMessageName => "marker";
        protected override string Info => $"id: {Id,3}";
        protected override int ByteDataLength => 2;

        public int Id { get; }

        public ImuseMarker(byte[] data) : base(data)
        {
            Id = ImuseByteData[1];
        }
    }

    public class ImuseV2Marker : ImuseMessage
    {
        protected override string ImuseMessageName => "marker-v2";
        protected override string Info => $"id: {Id,3}";

        protected override bool HasChannel => false;

        public int Id { get; }

        public ImuseV2Marker(byte[] data) : base(data)
        {
            Id = ImuseByteData[0];
        }
    }

    public class ImuseSetLoop : ImuseMessage
    {
        protected override string ImuseMessageName => "set-loop";
        protected override string Info => $"count: {Count,5}, start-beat: {StartBeat,5}, start-tick: {StartTick,5}, end-beat: {EndBeat,5}, end-tick: {EndTick,5}";

        public int Count { get; }
        public int StartBeat { get; }
        public int StartTick { get; }
        public int EndBeat { get; }
        public int EndTick { get; }

        public ImuseSetLoop(byte[] data) : base(data)
        {
            Count = ImuseData[0] << 8 | ImuseData[1];
            StartBeat = ImuseData[2] << 8 | ImuseData[3];
            StartTick = ImuseData[4] << 8 | ImuseData[5];
            EndBeat = ImuseData[6] << 8 | ImuseData[7];
            EndTick = ImuseData[8] << 8 | ImuseData[9];
        }
    }

    public class ImuseClearLoop : ImuseMessage
    {
        protected override string ImuseMessageName => "clear-loop";
        protected override string Info => "";

        public ImuseClearLoop(byte[] data) : base(data)
        {
        }
    }

    public class ImuseLoadSetup : ImuseMessage
    {
        protected override string ImuseMessageName => "load-setup";
        protected override string Info => $"setup-number: {SetupNumber,5}";

        public int SetupNumber { get; }

        public ImuseLoadSetup(byte[] data) : base(data)
        {
            SetupNumber = ImuseData[0] << 8 | ImuseData[1];
        }
    }
}