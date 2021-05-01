using Jither.Imuse.Drivers;
using Jither.Imuse.Messages;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;

namespace Jither.Imuse
{
    /// <summary>
    /// A Part represents a single channel in a MIDI being played. It handles its playback properties
    /// (instrument/program, volume, panning, priority etc.), and takes care of sending the channel's
    /// events and playback changes to the driver.
    /// </summary>
    public class Part
    {
        private readonly Driver driver;
        private Player player;
        private Slot slot;

        public int Index { get; private set; }

        private int inputChannel;
        private bool enabled;
        private int priorityOffset;
        private int volume;
        private int pan;
        private int transpose;
        private int detune;

        private bool transposeLocked;
        private int modWheel;
        private int reverb;
        private int sustain;
        private int pitchBend;
        private int pitchBendRange;
        private int program;

        /// <summary>
        /// The channel whose input messages this part will handle.
        /// </summary>
        public int InputChannel
        {
            get => inputChannel;
            private set
            {
                inputChannel = value;
            }
        }

        /// <summary>
        /// Gets or sets whether this part sends note events (i.e. is audible).
        /// </summary>
        /// <remarks>
        /// Setting this property to <c>false</c> will notify the driver to stop all notes.
        /// </remarks>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled != value)
                {
                    enabled = value;
                    if (!enabled && slot != null)
                    {
                        driver.StopAllNotes(slot);
                        // TODO: Release slot? (Is it needed? We're about to reassign slots, which should yank the slot
                        // away from this part, because it's disabled.
                    }
                    // We've just enabled or disabled this part, so it may need a slot - or may have one to give away.
                    // Unless it's TransposeLocked, in which case it doesn't have or need a slot:
                    if (!TransposeLocked)
                    {
                        NotifySlotReassignmentRequired();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the priority of this part.
        /// </summary>
        /// <remarks>
        /// This is added to player's effective priority to determine whether the part gets a <see cref="Slot"/>. See <see cref="PriorityEffective"/>.
        /// </remarks>
        public int PriorityOffset
        {
            get => priorityOffset;
            set
            {
                this.priorityOffset = value;
                if (!TransposeLocked)
                {
                    //Priority has changed, so may lose slot - or gain it.
                    NotifySlotReassignmentRequired();
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume of this part.
        /// </summary>
        /// <remarks>
        /// This is added to player's effective volume to determine final volume. See <see cref="VolumeEffective"/>.<br/>
        /// Setting this property will notify the driver that volume has changed.
        /// </remarks>
        public int Volume
        {
            get => volume;
            set
            {
                volume = value;
                driver.SetVolume(this);
            }
        }

        /// <summary>
        /// Gets or sets the panning of this part. This is a signed value between -64 and 63, 0 being centered.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that panning has changed.
        /// </remarks>
        public int Pan {
            get => pan;
            private set
            {
                pan = value;
                driver.SetPan(this);
            }
        }

        /// <summary>
        /// Gets or sets the transposition of this part. This is a signed value between -24 and 24, 0 being no transposition.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that transposition has changed.
        /// </remarks>
        public int Transpose {
            get => transpose;
            set
            {
                transpose = value;
                driver.SetPitchOffset(this);
            }
        }

        /// <summary>
        /// Gets or sets detune of this part. This is a signed value.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that detune has changed.
        /// </remarks>
        public int Detune
        {
            get => detune;
            private set
            {
                detune = value;
                driver.SetPitchOffset(this);
            }
        }

        /// <summary>
        /// <c>true</c> for percussion parts, <c>false</c> for other instruments.
        /// </summary>
        public bool TransposeLocked
        {
            get => transposeLocked;
            set
            {
                transposeLocked = value;
            }
        }

        /// <summary>
        /// Gets or sets the modulation wheel controller value for this part.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that modulation wheel value has changed.
        /// </remarks>
        public int ModWheel
        {
            get => modWheel;
            private set
            {
                modWheel = value;
                driver.SetModWheel(this);
            }
        }

        public int Reverb
        {
            get => reverb;
            private set
            {
                reverb = value;
            }
        }

        /// <summary>
        /// Gets or sets sustain controller value for this part.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that sustain controller value has changed.
        /// </remarks>
        public int Sustain
        {
            get => sustain;
            private set
            {
                sustain = value;
                driver.SetSustain(this);
            }
        }

        /// <summary>
        /// Gets or sets pitch bend of this part.
        /// </summary>
        /// <remarks>
        /// Setting this property will notify the driver that pitch bend has changed.
        /// </remarks>
        public int PitchBend
        {
            get => pitchBend;
            private set
            {
                pitchBend = value;
                driver.SetPitchOffset(this);
            }
        }

        /// <summary>
        /// Gets or sets the pitch bend range of this part.
        /// </summary>
        public int PitchBendRange
        {
            get => pitchBendRange;
            private set
            {
                pitchBendRange = value;
            }
        }

        /// <summary>
        /// Gets or sets the program (patch) of this part.
        /// </summary>
        /// <remarks>
        /// Setting this property notifies the driver that program has changed.
        /// </remarks>
        public int Program
        {
            get => program;
            set
            {
                program = value;
                driver.LoadRomSetup(this, program);
                MayRequireSlotReassignment();
            }
        }

        /// <summary>
        /// Indicates whether this part is currently assigned to a player.
        /// </summary>
        public bool IsInUse => player != null;

        /// <summary>
        /// The slot that currently handles output from this part. This may be null, if all slots are occupied by
        /// parts with higher priority - and is also null for percussion parts.
        /// </summary>
        public Slot Slot => slot;

        // A part needs a slot if it doesn't have one (duh), it is assigned to a player, it's enabled, and it's not
        // percussion (transposeLocked)
        public bool NeedsSlot => Slot == null && IsInUse && Enabled && !TransposeLocked;

        // Effective values (typically a combination of player's value and part's value)

        /// <summary>
        /// Effective priority for this part, based on player's priority and the part's priority offset. Limited to a number between 0 and 255.
        /// </summary>
        public int PriorityEffective => Math.Clamp(player?.Priority ?? 0 + PriorityOffset, 0, 255);

        /// <summary>
        /// Effective volume for this part, based on player's volume and the part's volume.
        /// </summary>
        public int VolumeEffective => (player.EffectiveVolume * (Volume + 1)) >> 7;

        /// <summary>
        /// Effective panning for this part, based on player's panning and the part's panning. Limited to -64 to 63.
        /// </summary>
        public int PanEffective => Math.Clamp(player.Pan + Pan, -64, 63);

        /// <summary>
        /// Effective transposition for this part, based on player's transposition and the part's transposition. Limited to between -12 and 12 semitones.
        /// </summary>
        public int TransposeEffective => TransposeLocked ? 0 : Math.Clamp(player.Transpose + Transpose, -12, 12);

        /// <summary>
        /// Effective detune for this part, based on player's detune and the part's detune. Limited to a number between -128 and 127.
        /// </summary>
        public int DetuneEffective => Math.Clamp(player.Detune + Detune, -128, 127);

        /// <summary>
        /// Combination of <see cref="PitchBend"/>, <see cref="DetuneEffective"/>, and <see cref="TransposeEffective"/>.
        /// </summary>
        /// <remarks>
        /// In practice, this is the value the driver will use to realise pitch bend, detune and transposition. This originally stems from the Roland MT-32's
        /// quirky behaviour when using actual detune or transposition.
        /// </remarks>
        public int PitchOffset => Math.Clamp(PitchBend + DetuneEffective + (TransposeEffective << 7), -0x800, 0x7ff);

        /// <summary>
        /// Event triggered from part to indicate that the part manager needs to reassign slots - e.g. due to a part being enabled/disabled, or getting its first
        /// program change.
        /// </summary>
        public event Action SlotReassignmentRequired;

        public Part(int index, Driver driver)
        {
            this.driver = driver;
            Index = index;
        }

        public void LinkPlayer(Player player)
        {
            this.player = player;
        }

        public void UnlinkPlayer()
        {
            this.player = null;
        }

        public void LinkSlot(Slot slot)
        {
            this.slot = slot;
        }

        public void UnlinkSlot()
        {
            this.slot = null;
        }

        public void Alloc(ImuseAllocPart alloc)
        {
            InputChannel = alloc.Channel;
            enabled = alloc.Enabled;
            priorityOffset = alloc.PriorityOffset;
            volume = alloc.Volume;
            pan = alloc.Pan;
            transpose = alloc.Transpose;
            transposeLocked = alloc.TransposeLocked;
            detune = alloc.Detune;
            modWheel = 0;
            sustain = 0;
            pitchBendRange = alloc.PitchBendRange;
            pitchBend = 0;
            reverb = alloc.Reverb ? 1 : 0;
            program = alloc.Program;
        }

        public void HandleEvent(ChannelMessage message)
        {
            switch (message)
            {
                case NoteOnMessage noteOn:
                    if (Enabled)
                    {
                        driver.StartNote(this, noteOn.Key, noteOn.Velocity);
                    }
                    break;
                case NoteOffMessage noteOff:
                    // Disabling a part sends all-notes-off, so skipping note-offs here should be fine (and is what the original does)
                    if (Enabled)
                    {
                        driver.StopNote(this, noteOff.Key);
                    }
                    break;
                case ControlChangeMessage controlChange:
                    switch (controlChange.Controller)
                    {
                        case MidiController.ModWheel:
                            ModWheel = controlChange.Value;
                            break;
                        case MidiController.ChannelVolume:
                            Volume = controlChange.Value;
                            break;
                        case MidiController.Pan:
                            Pan = controlChange.Value - 0x40; // Center
                            break;
                        case MidiController.Sustain:
                            Sustain = controlChange.Value;
                            break;
                    }
                    break;
                case ProgramChangeMessage programChange:
                    Program = programChange.Program;
                    break;
                case PitchBendChangeMessage pitchBend:
                    int bender = pitchBend.Bender - 0x2000; // Center around 0
                    PitchBend = (bender * PitchBendRange) >> 6;
                    break;
                default:
                    throw new ArgumentException($"Unexpected message to part: {message}");
            }
        }

        public void HandleEvent(ImuseMessage message)
        {
            switch (message)
            {
                case ImuseActiveSetup activeSetup:
                    ActiveSetup(activeSetup.Setup);
                    break;
                case ImuseStoredSetup storedSetup:
                    StoredSetup(storedSetup.SetupNumber, storedSetup.Setup);
                    break;
                case ImuseSetupBank:
                    // Not used by Roland
                    break;
                case ImuseSystemParam:
                    // Not used by Roland
                    break;
                case ImuseSetupParam setupParam:
                    SetupParam(setupParam.Number, setupParam.Value);
                    break;
                case ImuseMarker:
                    break;
                case ImuseLoadSetup loadSetup:
                    LoadStoredSetup(loadSetup);
                    break;

            }
        }

        public void StopAllNotes()
        {
            if (slot != null)
            {
                driver.StopAllNotes(slot);
            }
        }

        public void StopAllSustains()
        {
            if (Sustain != 0)
            {
                Sustain = 0;
            }
        }

        public void GetSustainNotes(HashSet<SustainedNote> notes)
        {
            if (slot != null)
            {
                driver.GetSustainNotes(slot, notes);
            }
        }

        public void ActiveSetup(byte[] setup)
        {
            driver.ActiveSetup(this, setup);
            MayRequireSlotReassignment();
        }

        public void StoredSetup(int setupNumber, byte[] setup)
        {
            // Should be done... elsewhere - not part-related
            driver.StoredSetup(setupNumber, setup);
        }

        private void LoadStoredSetup(ImuseLoadSetup loadSetup)
        {
            if (driver.LoadSetup(this, loadSetup.SetupNumber))
            {
                MayRequireSlotReassignment();
            }
        }

        public void SetupParam(int setupNumber, int value)
        {
            driver.SetupParam(this, setupNumber, value);
        }

        private void MayRequireSlotReassignment()
        {
            // This is called by methods that change properties that will result in TransposeLocked
            // being set true (e.g. program change). If that means a change from being false, notify
            // part manager that slots need to be reassigned (because this part will need one).
            if (TransposeLocked)
            {
                TransposeLocked = false;
                NotifySlotReassignmentRequired();
            }
        }

        private void NotifySlotReassignmentRequired()
        {
            SlotReassignmentRequired?.Invoke();
        }
    }
}
