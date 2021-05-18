using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using Jither.Midi.Files;
using System;
using Jither.Logging;
using System.Text;
using Jither.Imuse.Parts;

namespace Jither.Imuse.Messages
{
    /// <summary>
    /// Parses MIDI sysex messages with manufacturer ID 0x7d (iMUSE messages) during MIDI file loading.
    /// </summary>
    public class ImuseSysexParser : ISysexParser
    {
        public int ManufacturerId => 0x7d;

        public SysexMessage Parse(byte[] data)
        {
            return data[1] switch
            {
                0x00 => data.Length == 4 ? new ImuseV3Marker(data) : new ImuseAllocPart(data),
                0x01 => data.Length == 11 ? new ImuseV3HookJump(data) : new ImuseDeallocPart(data),
                0x02 => new ImuseDeallocAllParts(data),
                0x03 => new ImuseV3Text(data),
                0x10 => new ImuseActiveSetup(data),
                0x11 => new ImuseStoredSetup(data),
                0x12 => new ImuseSetupBank(data),
                0x20 => new ImuseSystemParam(data),
                0x21 => new ImuseSetupParam(data),
                0x30 => new ImuseHookJump(data),
                0x31 => new ImuseHookTranspose(data),
                0x32 => new ImuseHookPartEnable(data),
                0x33 => new ImuseHookPartVolume(data),
                0x34 => new ImuseHookPartProgramChange(data),
                0x35 => new ImuseHookPartTranspose(data),
                0x40 => new ImuseMarker(data),
                0x50 => new ImuseSetLoop(data),
                0x51 => new ImuseClearLoop(data),
                0x60 => new ImuseLoadSetup(data),
                _ => new ImuseUnknown(data)
            };
        }
    }

    /// <summary>
    /// Base class for iMUSE MIDI (sysex) messages.
    /// </summary>
    public abstract class ImuseMessage : SysexMessage
    {
        protected static readonly Logger logger = LogProvider.Get(nameof(ImuseMessage));
        public override string Name => "imuse";
        public override string Parameters => $"{ImuseMessageName,-20} {Info}";
        protected abstract string Info { get; }
        public abstract string ImuseMessageName { get; }

        public override string ToString() => $"{Name,-11} {Channel,2}  {Parameters}";

        public byte[] ImuseData { get; private set; }
        public ArraySegment<byte> ImuseByteData { get; }

        public int Channel { get; }

        protected virtual bool HasChannel => true;

        protected ImuseMessage(byte[] data, int byteDataLength) : base(data)
        {
            // Skip manufacturer ID and type
            int dataIndex = 2;

            // A few rare sound files have different sized iMUSE markers (RUDE8 in MI2) - we truncate the data in that case.
            int bytesLeftForData = data.Length - 3;
            if (byteDataLength > bytesLeftForData)
            {
                logger.Warning($"Found iMUSE {ImuseMessageName} with less than {byteDataLength} bytes of data ({bytesLeftForData})");
                byteDataLength = bytesLeftForData;
            }

            // The first few bytes after that are 7 bit data - length depends on message type.
            // Note that for v3, all (well, both) iMUSE messages only have byte data.
            ImuseByteData = new ArraySegment<byte>(data, dataIndex, byteDataLength);

            // In v1, the first byte is the channel
            if (HasChannel)
            {
                if ((ImuseByteData[0] & 0xf0) != 0)
                {
                    throw new MidiMessageException($"iMuse message has channel high nibble set: {ImuseByteData[0]:x2} - full sysex: {data.ToHex()}");
                }
                Channel = ImuseByteData[0];
            }

            dataIndex += byteDataLength;

            // The remainder is bytes with nibbles (4 bits) distributed into two bytes
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

    /// <summary>
    /// Unrecognized iMUSE MIDI message.
    /// </summary>
    public class ImuseUnknown : ImuseMessage
    {
        protected override string Info => Data.ToHex();
        public override string ImuseMessageName => "unknown";
        protected override bool HasChannel => false;

        public ImuseUnknown(byte[] data) : base(data, data.Length - 3)
        {
        }
    }

    /// <summary>
    /// iMUSE alloc_part MIDI (sysex) message. Signals to attempt to allocate a Part (input channel) to a free Slot on the Player.
    /// It also includes initial playback parameters for the part.
    /// </summary>
    public class ImuseAllocPart : ImuseMessage, IPartAllocation
    {
        public const int transposeLockedFlag = -128;

        public override string ImuseMessageName => "alloc-part";
        protected override string Info => $"enabled: {Enabled,5}, reverb: {Reverb}, prioffs: {PriorityOffset,3}, vol: {Volume,3}, pan: {Pan,3}, trans: {(TransposeLocked ? "lock" : Transpose),4}, detune: {Detune,3}, pbr: {PitchBendRange,3}, pgm: {Program,3}";

        public bool Enabled { get; }
        public int Reverb { get; }
        public int PriorityOffset { get; }
        public int Volume { get; }
        public int Pan { get; }
        public int Transpose { get; }
        public int Detune { get; }
        public int PitchBendRange { get; }
        public int Program { get; }
        public bool TransposeLocked => Transpose == transposeLockedFlag;

        public ImuseAllocPart(byte[] data) : base(data, 1)
        {
            Enabled = (ImuseData[0] & 0x01) != 0;
            // Reverb in iMUSE v1 (the only one with "alloc-part" messages) is actually a bool - but that's because
            // it's targeted at MT-32. Our Roland driver looks at the integer as a boolean (0 = false, everything else = true),
            // while other drivers expect an integer value. For compatibiity with iMUSE v2, we treat it as int here.
            Reverb = (ImuseData[0] & 0x02) != 0 ? 1 : 0;
            PriorityOffset = ImuseData[1];
            Volume = ImuseData[2];
            Pan = (sbyte)ImuseData[3];
            Transpose = (sbyte)ImuseData[4];
            Detune = (sbyte)ImuseData[5];
            PitchBendRange = ImuseData[6];
            Program = ImuseData[7];
        }
    }

    /// <summary>
    /// iMUSE dealloc_part MIDI (sysex) message. Signals to deallocate a Part (input channel) from its Slot on the Player.
    /// </summary>
    public class ImuseDeallocPart : ImuseMessage
    {
        public override string ImuseMessageName => "dealloc-part";
        protected override string Info => "";

        public ImuseDeallocPart(byte[] data) : base(data, 1)
        {
        }
    }

    /// <summary>
    /// iMUSE hook_jump MIDI (sysex) message for iMUSE v3 (Sam and Max). Signals to jump to a new position in the soundfile, if the given hook is set.
    /// </summary>
    public class ImuseV3Text : ImuseMessage
    {
        public override string ImuseMessageName => "text-v3";
        protected override string Info => $"{Text}";
        protected override bool HasChannel => false;

        public string Text { get; }

        public ImuseV3Text(byte[] data) : base(data, data.Length - 3)
        {
            Text = Encoding.ASCII.GetString(ImuseByteData);
        }
    }

    /// <summary>
    /// iMUSE dealloc_all_parts MIDI (sysex) message. Signals to deallocate all parts from their Slots on the Player. This is
    /// usually included at the start of a sound file, before allocating new parts with <see cref="ImuseAllocPart"/>.
    /// </summary>
    public class ImuseDeallocAllParts : ImuseMessage
    {
        public override string ImuseMessageName => "dealloc-all-parts";
        protected override string Info => "";

        public ImuseDeallocAllParts(byte[] data) : base(data, 1)
        {
        }
    }

    /// <summary>
    /// iMUSE active_setup MIDI (sysex) message. Signals to send instrument setup data to the output device driver.
    /// </summary>
    public class ImuseActiveSetup : ImuseMessage
    {
        public override string ImuseMessageName => "active-setup";
        protected override string Info => $"hw-id: {HardwareId,3}, setup: {Setup.ToHex()}";

        public HardwareId HardwareId { get; }
        public byte[] Setup { get; }

        public ImuseActiveSetup(byte[] data) : base(data, 2)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            Setup = ImuseData;
        }
    }

    /// <summary>
    /// iMUSE stored_setup MIDI (sysex) message. Signals to send instrument setup data to the output device driver, and
    /// stores it for later reloading.
    /// </summary>
    public class ImuseStoredSetup : ImuseMessage
    {
        public override string ImuseMessageName => "stored-setup";
        protected override string Info => $"hw-id: {HardwareId,3}, setup-number: {SetupNumber,3}, setup: {Setup.ToHex()}";

        public HardwareId HardwareId { get; }
        public int SetupNumber { get; }
        public byte[] Setup { get; }

        public ImuseStoredSetup(byte[] data) : base(data, 3)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            SetupNumber = ImuseByteData[2];
            Setup = ImuseData;
        }
    }

    public class ImuseSetupBank : ImuseMessage
    {
        public override string ImuseMessageName => "setup-bank";
        protected override string Info => "";

        public ImuseSetupBank(byte[] data) : base(data, 1)
        {
        }
    }

    public class ImuseSystemParam : ImuseMessage
    {
        public override string ImuseMessageName => "system-param";
        protected override string Info => "";

        public ImuseSystemParam(byte[] data) : base(data, 1)
        {
        }
    }

    /// <summary>
    /// iMUSE setup_param MIDI (sysex) message. Signals to set a single instrument parameter on the output device driver.
    /// </summary>
    public class ImuseSetupParam : ImuseMessage
    {
        public override string ImuseMessageName => "setup-param";
        protected override string Info => $"hw-id: {HardwareId,3}, param number: {Number,5}, value: {Value,5}";

        public HardwareId HardwareId { get; }
        public int Number { get; }
        public int Value { get; }
        public ImuseSetupParam(byte[] data) : base(data, 2)
        {
            HardwareId = (HardwareId)ImuseByteData[1];
            Number = ImuseData[0] << 8 | ImuseData[1];
            Value = ImuseData[2] << 8 | ImuseData[3];
        }
    }

    /// <summary>
    /// Base class for iMUSE hook MIDI (sysex) message.
    /// </summary>
    public abstract class ImuseHook : ImuseMessage
    {
        public int Hook { get; protected set; }
        public abstract HookType Type { get; }

        protected ImuseHook(byte[] data, int byteDataLength) : base(data, byteDataLength)
        {
        }
    }

    /// <summary>
    /// iMUSE hook_jump MIDI (sysex) message. Signals to jump to a new position in the soundfile, if the given hook is set.
    /// </summary>
    public class ImuseHookJump : ImuseHook
    {
        public override string ImuseMessageName => "hook-jump";
        protected override string Info => $"hook: {Hook,3}, chunk: {Chunk,5}, beat: {Beat,5}, tick: {Tick,5}";
        public override HookType Type => Imuse.HookType.Jump;

        public int Chunk { get; }
        public int Beat { get; }
        public int Tick { get; }

        public ImuseHookJump(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            // Yes, ImuseData is full 8-bit bytes (unpacked from nibbles), so << 8 is correct.
            Chunk = ImuseData[1] << 8 | ImuseData[2];
            Beat = ImuseData[3] << 8 | ImuseData[4];
            Tick = ImuseData[5] << 8 | ImuseData[6];
        }
    }

    /// <summary>
    /// iMUSE hook_jump MIDI (sysex) message for iMUSE v3 (Sam and Max). Signals to jump to a new position in the soundfile, if the given hook is set.
    /// </summary>
    public class ImuseV3HookJump : ImuseHook
    {
        public override string ImuseMessageName => "hook-jump-v3";
        protected override string Info => $"hook: {Hook,3}, chunk: {Chunk,3}, measure: {Measure,5}, beat: {Beat,3}, tick: {Tick,3}, sustain: {Sustain}";
        protected override bool HasChannel => false;

        public override HookType Type => Imuse.HookType.Jump;

        public int Chunk { get; }
        public int Measure { get; }
        public int Beat { get; }
        public int Tick { get; }
        public bool Sustain { get; }

        public ImuseV3HookJump(byte[] data) : base(data, 8)
        {
            Hook = ImuseByteData[0];
            Chunk = ImuseByteData[1] - 1; // Chunk is 1-indexed rather than 0-indexed in v3. We keep it 0-indexed.
            Measure = ImuseByteData[2] << 7 | ImuseByteData[3]; // Measure is 1-indexed
            Beat = ImuseByteData[4];
            Tick = ImuseByteData[5] << 7 | ImuseByteData[6];

            if (ImuseByteData[7] > 1)
            {
                throw new MidiMessageException($"Unexpected sustain value in v3 hook-jump: {ImuseByteData[7]:x2}");
            }

            Sustain = ImuseByteData[7] == 1;
        }
    }

    /// <summary>
    /// iMUSE hook_transpose MIDI (sysex) message. Signals to transpose all Parts in the Player, if the given hook is set.
    /// </summary>
    public class ImuseHookTranspose : ImuseHook
    {
        public override string ImuseMessageName => "hook-transpose";
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public override HookType Type => Imuse.HookType.Transpose;

        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookTranspose(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = (sbyte)ImuseData[2];
        }
    }

    /// <summary>
    /// iMUSE hook_part_enable MIDI (sysex) message. Signals to enable or disable a single Part in the Player, if the given hook is set.
    /// When disabled, all output from the part is suppressed.
    /// </summary>
    public class ImuseHookPartEnable : ImuseHook
    {
        public override string ImuseMessageName => "hook-part-enable";
        protected override string Info => $"hook: {Hook,3}, state: {Enabled,3}";

        public override HookType Type => Imuse.HookType.PartEnable;

        public int Enabled { get; }

        public ImuseHookPartEnable(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            Enabled = ImuseData[1];
        }
    }

    /// <summary>
    /// iMUSE hook_part_vol MIDI (sysex) message. Signals to change the volume of a single Part in the Player, if the given hook is set.
    /// </summary>
    public class ImuseHookPartVolume : ImuseHook
    {
        public override string ImuseMessageName => "hook-part-vol";
        protected override string Info => $"hook: {Hook,3}, vol: {Volume,3}";

        public override HookType Type => Imuse.HookType.PartVolume;

        public int Volume { get; }

        public ImuseHookPartVolume(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            Volume = ImuseData[1];
        }
    }

    /// <summary>
    /// iMUSE hook_part_pgmch MIDI (sysex) message. Signals to change the program of a single Part in the Player, if the given hook is set.
    /// </summary>
    public class ImuseHookPartProgramChange : ImuseHook
    {
        public override string ImuseMessageName => "hook-part-pgmch";
        protected override string Info => $"hook: {Hook,3}, vol: {Program,3}";

        public override HookType Type => Imuse.HookType.PartProgramChange;

        public int Program { get; }

        public ImuseHookPartProgramChange(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            Program = ImuseData[1];
        }
    }

    /// <summary>
    /// iMUSE hook_part_transpose MIDI (sysex) message. Signals to transpose a single Part in the Player, if the given hook is set.
    /// </summary>
    public class ImuseHookPartTranspose : ImuseHook
    {
        public override string ImuseMessageName => "hook-part-transpose";
        protected override string Info => $"hook: {Hook,3}, relative: {Relative,3}, interval: {Interval,3}";

        public override HookType Type => Imuse.HookType.PartTranspose;

        public int Relative { get; }
        public int Interval { get; }

        public ImuseHookPartTranspose(byte[] data) : base(data, 1)
        {
            Hook = ImuseData[0];
            Relative = ImuseData[1];
            Interval = (sbyte)ImuseData[2];
        }
    }

    /// <summary>
    /// iMUSE marker MIDI (sysex) message. Signals to trigger iMUSE (script) commands queued for the Player.
    /// </summary>
    public class ImuseMarker : ImuseMessage
    {
        public override string ImuseMessageName => "marker";
        protected override string Info => $"id: {Id,3}";

        public int Id { get; }

        public ImuseMarker(byte[] data) : base(data, 2)
        {
            // Markers have been spotted with a single byte of data...
            if (ImuseByteData.Count == 2)
            {
                Id = ImuseByteData[1];
            }
            else
            {
                logger.Warning("Short marker. Assuming ID 0.");
            }
        }
    }

    /// <summary>
    /// iMUSE marker MIDI (sysex) message for iMUSE v3 (Sam and Max). Signals to trigger iMUSE (script) commands queued for the Player.
    /// </summary>
    public class ImuseV3Marker : ImuseMessage
    {
        public override string ImuseMessageName => "marker-v3";
        protected override string Info => $"id: {Id,3}";

        protected override bool HasChannel => false;

        public int Id { get; }

        public ImuseV3Marker(byte[] data) : base(data, 1)
        {
            Id = ImuseByteData[0];
        }
    }

    /// <summary>
    /// iMUSE set_loop MIDI (sysex) message. Signals to the sequencer to start looping between the given start position and end position in the current track.
    /// </summary>
    public class ImuseSetLoop : ImuseMessage
    {
        public override string ImuseMessageName => "set-loop";
        protected override string Info => $"count: {Count,5}, start-beat: {StartBeat,5}, start-tick: {StartTick,5}, end-beat: {EndBeat,5}, end-tick: {EndTick,5}";

        public int Count { get; }
        public int StartBeat { get; }
        public int StartTick { get; }
        public int EndBeat { get; }
        public int EndTick { get; }

        public ImuseSetLoop(byte[] data) : base(data, 1)
        {
            Count = ImuseData[0] << 8 | ImuseData[1];
            StartBeat = ImuseData[2] << 8 | ImuseData[3];
            StartTick = ImuseData[4] << 8 | ImuseData[5];
            EndBeat = ImuseData[6] << 8 | ImuseData[7];
            EndTick = ImuseData[8] << 8 | ImuseData[9];
        }
    }

    /// <summary>
    /// iMUSE clear_loop MIDI (sysex) message. Signals to the sequencer to stop looping.
    /// </summary>
    public class ImuseClearLoop : ImuseMessage
    {
        public override string ImuseMessageName => "clear-loop";
        protected override string Info => "";

        public ImuseClearLoop(byte[] data) : base(data, 1)
        {
        }
    }

    /// <summary>
    /// iMUSE load_setup MIDI (sysex) message. Signals to load given previously stored instrument setup into the Part.
    /// </summary>
    public class ImuseLoadSetup : ImuseMessage
    {
        public override string ImuseMessageName => "load-setup";
        protected override string Info => $"setup-number: {SetupNumber,5}";

        public int SetupNumber { get; }

        public ImuseLoadSetup(byte[] data) : base(data, 1)
        {
            SetupNumber = ImuseData[0] << 8 | ImuseData[1];
        }
    }
}
