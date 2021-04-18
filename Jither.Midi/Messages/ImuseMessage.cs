using Jither.Midi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Messages
{
    public enum ImuseMessageType
    {
        AllocPart = 0x00,
        DeallocPart = 0x01,
        DeallocAllParts = 0x02,
        ActiveSetup = 0x10,
        StoredSetup = 0x11,
        SetupBank = 0x12,
        SystemParam = 0x20,
        SetupParam = 0x21,
        HookJump = 0x30,
        HookTranspose = 0x31,
        HookPartEnable = 0x32,
        HookPartVol = 0x33,
        HookPartPgmch = 0x34,
        HookPartTranspose = 0x35,
        Marker = 0x40,
        SetLoop = 0x50,
        ClearLoop = 0x51,
        LoadSetup = 0x60
    }

    public enum HardwareId
    {
        Adlib = 1,
        Roland = 2,
        Speaker = 3,
        Mac = 4,
        GeneralMidi = 5
    }

    public abstract class ImuseMessage : SysexMessage
    {
        public override string Name => "imuse";
        public override string Parameters => $"{ImuseName,-20} {Info}";
        protected abstract string Info { get; }
        protected virtual string ImuseName => StringConverter.PascalToKebabCase(Type.ToString());

        public override string ToString() => $"{Name,-11} {Channel,2}  {Parameters}";

        public ImuseMessageType Type { get; }
        public byte[] ImuseData { get; }
        public ArraySegment<byte> ImuseByteData { get; }

        public int Channel { get; }

        protected virtual int ByteDataLength => 1;

        public ImuseMessage(byte[] data) : base(data)
        {
            // Skip manufacturer ID
            int source = 1;
            Type = (ImuseMessageType)data[source++];
            // The first few bytes after that are 7 bit data - length depends on message type
            ImuseByteData = new ArraySegment<byte>(data, source, ByteDataLength);
            source += ByteDataLength;
            if ((ImuseByteData[0] & 0xf0) != 0)
            {
                throw new MidiMessageException($"iMuse message has channel high nibble set: {ImuseByteData[0]:x2}");
            }
            Channel = ImuseByteData[0];

            // The remainder is bytes with nibbles distributed into two bytes
            int nibblesEnd = data.Length - 1;

            ImuseData = new byte[(data.Length - source) / 2];

            int dest = 0;

            while (source < nibblesEnd)
            {
                byte hi = data[source++];
                if ((hi & 0xf0) > 0)
                {
                    throw new MidiMessageException($"iMuse message data has high nibble set: {hi:x2}");
                }
                byte unpacked = hi;
                if (source < nibblesEnd)
                {
                    byte lo = data[source++];
                    if ((lo & 0xf0) > 0)
                    {
                        throw new MidiMessageException($"iMuse message data has high nibble set: {hi:x2}");
                    }
                    unpacked = (byte)(unpacked << 4 | (lo & 0xf));
                }
                ImuseData[dest++] = unpacked;
            }
            if (data[source] != 0xf7)
            {
                throw new MidiMessageException($"Expected end of sysex (f7), but found: {data[source]:x2}");
            }
        }

        public static ImuseMessage Create(byte[] data)
        {
            return (ImuseMessageType)data[1] switch
            {
                ImuseMessageType.AllocPart => new ImuseAllocPart(data),
                ImuseMessageType.DeallocPart => new ImuseDeallocPart(data),
                ImuseMessageType.DeallocAllParts => new ImuseDeallocAllParts(data),
                ImuseMessageType.ActiveSetup => new ImuseActiveSetup(data),
                ImuseMessageType.StoredSetup => new ImuseStoredSetup(data),
                ImuseMessageType.SetupBank => new ImuseSetupBank(data),
                ImuseMessageType.SystemParam => new ImuseSystemParam(data),
                ImuseMessageType.SetupParam => new ImuseSetupParam(data),
                ImuseMessageType.HookJump => new ImuseHookJump(data),
                ImuseMessageType.HookTranspose => new ImuseHookTranspose(data),
                ImuseMessageType.HookPartEnable => new ImuseHookPartEnable(data),
                ImuseMessageType.HookPartVol => new ImuseHookPartVol(data),
                ImuseMessageType.HookPartPgmch => new ImuseHookPartPgmch(data),
                ImuseMessageType.HookPartTranspose => new ImuseHookPartTranspose(data),
                ImuseMessageType.Marker => new ImuseMarker(data),
                ImuseMessageType.SetLoop => new ImuseSetLoop(data),
                ImuseMessageType.ClearLoop => new ImuseClearLoop(data),
                ImuseMessageType.LoadSetup => new ImuseLoadSetup(data),
                _ => new ImuseUnknown(data)
            };
        }
    }

    public class ImuseUnknown : ImuseMessage
    {
        protected override string Info => Data.ToHex();
        protected override string ImuseName => "unknown";

        public ImuseUnknown(byte[] data) : base(data)
        {
        }
    }
    public class ImuseAllocPart : ImuseMessage
    {
        public const int transposeLockedFlag = -128;

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
        protected override string Info => "";

        public ImuseDeallocPart(byte[] data) : base(data)
        {
        }
    }

    public class ImuseDeallocAllParts : ImuseMessage
    {
        protected override string Info => "";

        public ImuseDeallocAllParts(byte[] data) : base(data)
        {
        }
    }

    public class ImuseActiveSetup : ImuseMessage
    {
        public HardwareId HardwareId { get; }
        public byte[] Setup { get; }

        protected override int ByteDataLength => 2;
        protected override string Info => $"hw-id: {HardwareId,3}, setup: {Setup.ToHex()}";

        public ImuseActiveSetup(byte[] data) : base(data)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            Setup = ImuseData;
        }
    }

    public class ImuseStoredSetup : ImuseMessage
    {
        public HardwareId HardwareId { get; }
        public int SetupNumber { get; }
        public byte[] Setup { get; }

        protected override int ByteDataLength => 3;
        protected override string Info => $"hw-id: {HardwareId,3}, setup-number: {SetupNumber,3}, setup: {Setup.ToHex()}";

        public ImuseStoredSetup(byte[] data) : base(data)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            SetupNumber = ImuseByteData[2];
            Setup = ImuseData;
        }
    }

    public class ImuseSetupBank : ImuseMessage
    {
        protected override string Info => "";

        public ImuseSetupBank(byte[] data) : base(data)
        {
        }
    }

    public class ImuseSystemParam : ImuseMessage
    {
        protected override string Info => "";

        public ImuseSystemParam(byte[] data) : base(data)
        {
        }
    }

    public class ImuseSetupParam : ImuseMessage
    {
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
        protected override string Info => $"hook: {Hook,3}, chunk: {Chunk,5}, beat: {Beat,5}, tick: {Tick,5}";

        public int Hook { get; }
        public int Chunk { get; }
        public int Beat { get; }
        public int Tick { get; }

        public ImuseHookJump(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Chunk = ImuseData[1] << 8 | ImuseData[2];
            Beat = ImuseData[3] << 8 | ImuseData[4];
            Tick = ImuseData[5] << 8 | ImuseData[6];
        }
    }

    public class ImuseHookTranspose : ImuseMessage
    {
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public int Hook { get; }
        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookTranspose(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = ImuseData[2];
        }
    }

    public class ImuseHookPartEnable : ImuseMessage
    {
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
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public int Hook { get; }
        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookPartTranspose(byte[] data) : base(data)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = ImuseData[2];
        }
    }

    public class ImuseMarker : ImuseMessage
    {
        protected override string Info => $"id: {Id,3}";
        protected override int ByteDataLength => 2;

        public int Id { get; }

        public ImuseMarker(byte[] data) : base(data)
        {
            Id = ImuseByteData[1];
        }
    }

    public class ImuseSetLoop : ImuseMessage
    {
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
        protected override string Info => "";

        public ImuseClearLoop(byte[] data) : base(data)
        {
        }
    }

    public class ImuseLoadSetup : ImuseMessage
    {
        protected override string Info => $"setup-number: {SetupNumber,5}";

        public int SetupNumber { get; }

        public ImuseLoadSetup(byte[] data) : base(data)
        {
            SetupNumber = ImuseData[0] << 8 | ImuseData[1];
        }
    }


}
