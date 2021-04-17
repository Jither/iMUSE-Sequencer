using Jither.Midi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Messages
{
    public abstract class MidiMessage
    {
        /// <summary>
        /// Descriptive name of the message
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// String describing the parameters of the message.
        /// </summary>
        public abstract string Parameters { get; }
        
        /// <summary>
        /// The message as an integer <i>in little endian order</i> (i.e. status byte in the least significant byte)
        /// </summary>
        public abstract int RawMessage { get; }

        public override string ToString() => $"{Name,-14}  {Parameters}";
    }

    public abstract class ChannelMessage : MidiMessage
    {
        public int Channel { get; set; }

        public override int RawMessage => (Command | Channel) | (RawData << 8);
        
        protected abstract byte Command { get; }
        protected abstract int RawData { get; }

        protected ChannelMessage(int channel)
        {
            Channel = channel;
        }

        public override string ToString() => $"{Name,-11} {Channel,2}  {Parameters}";
    }

    public class NoteOffMessage : ChannelMessage
    {
        public override string Name => "note-off";
        public override string Parameters => $"{MidiHelper.NoteNumberToName(Key),-4} {Velocity,3}";
        protected override byte Command => 0x80;
        protected override int RawData => Key | (Velocity << 8);

        public byte Key { get; set; }
        public byte Velocity { get; set; }

        public NoteOffMessage(int channel, byte key, byte velocity) : base(channel)
        {
            Key = key;
            Velocity = velocity;
        }
    }

    public class NoteOnMessage : ChannelMessage
    {
        public override string Name => "note-on";
        public override string Parameters => $"{MidiHelper.NoteNumberToName(Key),-4} {Velocity,3}";
        protected override byte Command => 0x90;
        protected override int RawData => Key | (Velocity << 8);

        public byte Key { get; set; }
        public byte Velocity { get; set; }

        public NoteOnMessage(int channel, byte key, byte velocity) : base(channel)
        {
            Key = key;
            Velocity = velocity;
        }
    }

    public class PolyPressure : ChannelMessage
    {
        public override string Name => "poly-press";
        public override string Parameters => $"{MidiHelper.NoteNumberToName(Key),-4} {Pressure,3}";
        protected override byte Command => 0xa0;
        protected override int RawData => Key | (Pressure << 8);

        public byte Key { get; set; }
        public byte Pressure { get; set; }

        public PolyPressure(int channel, byte key, byte pressure) : base(channel)
        {
            Key = key;
            Pressure = pressure;
        }
    }

    public class ControlChangeMessage : ChannelMessage
    {
        public override string Name => "ctrl-chng";
        public override string Parameters => $"{MidiHelper.GetControllerName(Controller),-20} {Value,3}";

        public MidiController Controller { get; set; }
        public byte Value { get; set; }
        protected override byte Command => 0xb0;
        protected override int RawData => (byte)Controller | (Value << 8);

        protected ControlChangeMessage(int channel, byte controller, byte value) : base(channel)
        {
            Controller = (MidiController)controller;
            Value = value;
        }

        public static ControlChangeMessage Create(int channel, MidiController controller, byte value)
        {
            return Create(channel, (byte)controller, value);
        }

        public static ControlChangeMessage Create(int channel, byte controller, byte value)
        {
            return controller switch
            {
                0x78 => new AllSoundOffMessage(channel, controller, value),
                0x79 => new ResetAllMessage(channel, controller, value),
                0x7a => new LocalControlMessage(channel, controller, value),
                0x7b => new AllNotesOffMessage(channel, controller, value),
                0x7c => new OmniOffMessage(channel, controller, value),
                0x7d => new OmniOnMessage(channel, controller, value),
                0x7e => new PolyOffMessage(channel, controller, value),
                0x7f => new PolyOnMessage(channel, controller, value),
                _ => new ControlChangeMessage(channel, controller, value)
            };
        }
    }

    public abstract class ChannelModeMessage : ControlChangeMessage
    {
        public override string Name => "chan-mode";
        public override string Parameters => $"{TypeName,-20} {Value}";
        public abstract string TypeName { get; }

        protected ChannelModeMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class AllSoundOffMessage : ChannelModeMessage
    {
        public override string TypeName => "all-sound-off";

        public AllSoundOffMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class ResetAllMessage : ChannelModeMessage
    {
        public override string TypeName => "reset-all";

        public ResetAllMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class LocalControlMessage : ChannelModeMessage
    {
        public override string TypeName => "local-control";

        public LocalControlMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class AllNotesOffMessage : ChannelModeMessage
    {
        public override string TypeName => "all-notes-off";

        public AllNotesOffMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class OmniOffMessage : ChannelModeMessage
    {
        public override string TypeName => "omni-off";

        public OmniOffMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class OmniOnMessage : ChannelModeMessage
    {
        public override string TypeName => "omni-on";

        public OmniOnMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class PolyOffMessage : ChannelModeMessage
    {
        public override string TypeName => "poly-off";

        public PolyOffMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class PolyOnMessage : ChannelModeMessage
    {
        public override string TypeName => "poly-on";

        public PolyOnMessage(int channel, byte controller, byte value) : base(channel, controller, value)
        {
        }
    }

    public class ProgramChangeMessage : ChannelMessage
    {
        public override string Name => "pgm-chng";
        public override string Parameters => $"{Program}";
        protected override byte Command => 0xc0;
        protected override int RawData => Program;

        public byte Program { get; set; }

        public ProgramChangeMessage(int channel, byte program) : base(channel)
        {
            Program = program;
        }
    }

    public class ChannelPressureMessage : ChannelMessage
    {
        public override string Name => "chan-press";
        public override string Parameters => $"{Pressure}";
        protected override byte Command => 0xd0;
        protected override int RawData => Pressure;

        public byte Pressure { get; set; }

        public ChannelPressureMessage(int channel, byte pressure) : base(channel)
        {
            Pressure = pressure;
        }
    }

    public class PitchBendChangeMessage : ChannelMessage
    {
        public override string Name => "pitch-bend";
        public override string Parameters => $"{Bender}";
        protected override byte Command => 0xe0;
        protected override int RawData => ((Bender & 0xff) << 8) | ((Bender & 0xff00) >> 8);

        public ushort Bender { get; set; }

        public PitchBendChangeMessage(int channel, ushort bender) : base(channel)
        {
            Bender = bender;
        }
    }

    public class SysexMessage : MidiMessage
    {
        public override string Name => "sysex";
        public override string Parameters => $"{(Continuation ? "->" : "")}{Data.ToHex()}{(Unterminated ? " ->" : "")}";
        public override int RawMessage => throw new NotSupportedException("Sysex message is not representable as a 32-bit integer.");

        /// <summary>
        /// Actual data excluding starting F0/F7 and length (from MIDI file), but including terminating F7 (if any).
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Indicates whether this message is a continuation of a previous sysex message, i.e.
        /// the previous sysex message ended without F7, and this one started with F7.
        /// </summary>
        public bool Continuation { get; set; }

        /// <summary>
        /// Indicates whether this message is (expected to be) continued in further sysex message(s), i.e.
        /// this sysex message ends without F7, and is followed by further messages starting
        /// with F7.
        /// </summary>
        public bool Unterminated => Data[^1] != 0xf7;

        public SysexMessage(byte[] data, bool continuation = false)
        {
            Data = data;
            Continuation = continuation;
        }
    }

    public enum MetaType
    {
        SequenceNumber = 0x00,
        TextEvent = 0x01,
        CopyrightNotice = 0x02,
        SequenceName = 0x03,
        InstrumentName = 0x04,
        Lyric = 0x05,
        Marker = 0x06,
        CuePoint = 0x07,
        MidiChannelPrefix = 0x20,
        EndOfTrack = 0x2f,
        SetTempo = 0x51,
        SmpteOffset = 0x54,
        TimeSignature = 0x58,
        KeySignature = 0x59,
        SequencerSpecific = 0x7f
    }

    public class MetaMessage : MidiMessage
    {
        public override string Name => "meta";
        public override string Parameters => $"{TypeName,-20} {Info}";
        public override int RawMessage => throw new NotSupportedException("Meta message is not representable as a 32-bit integer.");

        public byte Type { get; }
        public byte[] Data { get; set; }

        public virtual string TypeName => Type.ToString("x2");
        public virtual string Info => Data.ToHex();

        protected MetaMessage(byte type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public static MetaMessage Create(byte type, byte[] data)
        {
            return (MetaType)type switch
            {
                MetaType.SequenceNumber => new SequenceNumberMessage(type, data),
                MetaType.TextEvent => new TextEventMessage(type, data),
                MetaType.CopyrightNotice => new CopyrightNoticeMessage(type, data),
                MetaType.SequenceName => new SequenceNameMessage(type, data),
                MetaType.InstrumentName => new InstrumentNameMessage(type, data),
                MetaType.Lyric => new LyricMessage(type, data),
                MetaType.Marker => new MarkerMessage(type, data),
                MetaType.CuePoint => new CuePointMessage(type, data),
                MetaType.MidiChannelPrefix => new MidiChannelPrefixMessage(type, data),
                MetaType.EndOfTrack => new EndOfTrackMessage(type, data),
                MetaType.SetTempo => new SetTempoMessage(type, data),
                MetaType.SmpteOffset => new SmpteOffsetMessage(type, data),
                MetaType.TimeSignature => new TimeSignatureMessage(type, data),
                MetaType.KeySignature => new KeySignatureMessage(type, data),
                MetaType.SequencerSpecific => new SequencerSpecificMessage(type, data),
                _ => new MetaMessage(type, data)
            };
        }
    }

    public class SequenceNumberMessage : MetaMessage
    {
        public int Number => (Data[0] << 8) | Data[1];
        public override string TypeName => "sequence-number";
        public override string Info => Number.ToString();

        public SequenceNumberMessage(byte type, byte[] data) : base(type, data)
        {

        }
    }

    public class MetaTextMessage : MetaMessage
    {
        // Assuming ASCII here (although SMF spec actually doesn't in all cases - e.g. text-event)
        public string Text => Encoding.ASCII.GetString(Data);
        public override string Info => Text;

        public MetaTextMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }


    public class TextEventMessage : MetaTextMessage
    {
        public override string TypeName => "text-event";

        public TextEventMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class CopyrightNoticeMessage : MetaTextMessage
    {
        public override string TypeName => "copyright-notice";

        public CopyrightNoticeMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class SequenceNameMessage : MetaTextMessage
    {
        public override string TypeName => "sequence-name";

        public SequenceNameMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class InstrumentNameMessage : MetaTextMessage
    {
        public override string TypeName => "instrument-name";

        public InstrumentNameMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class LyricMessage : MetaTextMessage
    {
        public override string TypeName => "lyric";

        public LyricMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class MarkerMessage : MetaTextMessage
    {
        public override string TypeName => "marker";

        public MarkerMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class CuePointMessage : MetaTextMessage
    {
        public override string TypeName => "cuepoint";

        public CuePointMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class MidiChannelPrefixMessage : MetaMessage
    {
        public override string TypeName => "midi-channel-prefix";
        public override string Info => Prefix.ToString();

        public byte Prefix => Data[0];

        public MidiChannelPrefixMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class EndOfTrackMessage : MetaMessage
    {
        public override string TypeName => "end-of-track";

        public EndOfTrackMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class SetTempoMessage : MetaMessage
    {
        public override string TypeName => "set-tempo";
        public override string Info => Tempo.ToString();

        public int Tempo => (Data[0] << 16) | (Data[1] << 8) | Data[2];

        public SetTempoMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class SmpteOffsetMessage : MetaMessage
    {
        public override string TypeName => "smpte-offset";
        public byte Hours => Data[0];
        public byte Minutes => Data[1];
        public byte Seconds => Data[2];
        public byte Frames => Data[3];
        public byte FractionalFrames => Data[4];
        public override string Info => $"{Hours:00}:{Minutes:00}:{Seconds:00}:{Frames:00}.{FractionalFrames:00}";

        public SmpteOffsetMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class TimeSignatureMessage : MetaMessage
    {
        public override string TypeName => "time-signature";
        public override string Info => $"{Numerator}/{Denominator}, {ClocksPerBeat} MIDI clocks per beat, 1 MIDI quarter-note = {ThirtySecondNotesPerMidiQuarterNote} notated 32nd notes";

        public int Numerator => Data[0];
        public int Denominator => 1 << Data[1];
        public int ClocksPerBeat => Data[2];
        public int ThirtySecondNotesPerMidiQuarterNote => Data[3];

        

        public TimeSignatureMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class KeySignatureMessage : MetaMessage
    {
        public override string TypeName => "key-signature";
        public override string Info => $"{MidiHelper.GetKeySignatureName(Accidentals, Mode)} ({Math.Abs(Accidentals)} {(Accidentals > 0 ? "sharps" : "flats")})";

        public int Accidentals => Data[0];
        public int Mode => Data[1];

        public KeySignatureMessage(byte type, byte[] data) : base(type, data)
        {
        }
    }

    public class SequencerSpecificMessage : MetaMessage
    {
        public override string TypeName => "sequencer-specific";

        public SequencerSpecificMessage(byte type, byte[] data) : base(type, data)
        {

        }
    }
}
