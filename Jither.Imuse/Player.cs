using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Imuse.Messages;
using Jither.Imuse.Parts;
using Jither.Logging;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;

namespace Jither.Imuse
{
    public enum PlayerStatus
    {
        Off,
        On
    }

    public enum SoundStatus
    {
        NotPlaying,
        Playing,
        Pending
    }

    /// <summary>
    /// Players manage the playback of a single sound (MIDI) file.
    /// </summary>
    public class Player
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Player));
        private readonly int index;
        private readonly Driver driver;
        private readonly ImuseQueue queue;
        private readonly PartManager parts;
        private readonly Sequencer sequencer;
        private SoundFile file;
        public HookBlock HookBlock { get; }

        // List of parts currently used by this player
        private readonly PartsCollection linkedParts;
        private readonly ImuseOptions options;

        // Used for keeping track of number of jumps for a given hook - to allow limiting.
        private readonly Dictionary<ImuseHook, int> jumpsExecuted = new();

        public PlayerStatus Status { get; private set; }
        public int SoundId { get; private set; }
        public int Priority { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }
        public int EffectiveVolume => ((Volume + 1) * 127) >> 7; // TODO: "127" is actually system master volume

        public int Index => index;
        internal PartsCollection Parts => linkedParts;
        internal Sequencer Sequencer => sequencer;

        public Player(int index, Driver driver, PartManager parts, Sustainer sustainer, ImuseQueue queue, ImuseOptions options)
        {
            this.index = index;
            this.driver = driver;
            this.parts = parts;
            this.queue = queue;
            this.options = options;
            linkedParts = parts.GetCollection(this);
            Status = PlayerStatus.Off;
            HookBlock = new HookBlock(this);
            sequencer = new Sequencer(index, this, sustainer);
        }

        public void LinkPart(Part part)
        {
            part.LinkPlayer(this);
            linkedParts.Add(part);
        }

        public void UnlinkPart(Part part)
        {
            part.UnlinkPlayer();
            linkedParts.Remove(part);
        }

        /// <summary>
        /// Starts playback of sound. 
        /// </summary>
        public void Start(int id, SoundFile file)
        {
            logger.Verbose($"Starting sound {id} on player {index}");
            this.file = file;

            Status = PlayerStatus.On;
            SoundId = id;
            Priority = file.ImuseHeader?.Priority ?? 128; // iMUSE v2 has no iMUSE header, but assigns priority 128
            Volume = file.ImuseHeader?.Volume ?? 127;
            Pan = file.ImuseHeader?.Pan ?? 0;
            Transpose = file.ImuseHeader?.Transpose ?? 0;
            Detune = file.ImuseHeader?.Detune ?? 0;

            HookBlock.Clear();

            sequencer.Start(file.Midi);
        }

        /// <summary>
        /// Asks player to render events for the next tick. (They'll end up being sent to the driver).
        /// </summary>
        /// <returns>
        /// <c>false</c> if EndOfTrack message was reached during processing. Otherwise <c>true</c> (player active).
        /// </returns>
        public bool Tick()
        {
            return sequencer.Tick();
        }

        /// <summary>
        /// Stops playback of sound. Also called on EndOfTrack meta message.
        /// </summary>
        public void Stop()
        {
            sequencer.Stop();
            // TODO: StopFade();
            parts.DeallocAllParts(this);
            Status = PlayerStatus.Off;
            logger.Verbose($"Player {index} stopped.");
        }

        // TODO: Does anything actually need these "SetX" methods? (other than SetTranspose, used by hooks)
        // If they are, they could probably be made setters on the properties.
        
        /// <summary>
        /// Sets the priority of this Player, and updates priorities of all its parts.
        /// </summary>
        public void SetPriority(int priority)
        {
            Priority = priority;
            linkedParts.SetPriority();
        }

        /// <summary>
        /// Sets the volume of this Player, and updates the volume of all its parts.
        /// </summary>
        public bool SetVolume(int volume)
        {
            if (volume > 127)
            {
                return false;
            }
            Volume = volume;
            linkedParts.SetVolume();
            return true;
        }

        /// <summary>
        /// Sets the panning of this Player, and updates the panning of all its parts.
        /// </summary>
        public void SetPan(int pan)
        {
            Pan = pan;
            linkedParts.SetPan();
        }

        /// <summary>
        /// Sets transposition of this Player, and updates the transposition of all its parts.
        /// </summary>
        public bool SetTranspose(int interval, bool relative)
        {
            if (interval < -24 || interval > 24)
            {
                return false;
            }

            if (relative)
            {
                Transpose = Math.Clamp(interval + Transpose, -7, 7);
            }
            else
            {
                Transpose = interval;
            }

            linkedParts.SetTranspose();
            return true;
        }

        /// <summary>
        /// Sets the detune of this Player, and updates the detune of all its parts.
        /// </summary>
        public void SetDetune(int detune)
        {
            Detune = detune;
            linkedParts.SetDetune();
        }

        // UpdateMasterVolume not needed (probably) - we use property evaluation for effective volume
        
        public void StopAllSustains()
        {
            linkedParts.StopAllSustains();
        }

        public void StopAllNotesForJump()
        {
            // TODO: This is for iMUSE v3, but original iMUSE doesn't actually care about linked parts - it will reset all channels.
            // Is this better/OK?
            linkedParts.StopAllNotesForJump();
        }

        public void GetSustainNotes(HashSet<SustainedNote> notes)
        {
            foreach (var part in linkedParts)
            {
                part.GetSustainNotes(notes);
            }
        }

        /// <summary>
        /// Handles events from sequencer.
        /// </summary>
        public void HandleEvent(MidiMessage message)
        {
            if (message is ChannelMessage channelMessage)
            {
                linkedParts.HandleChannelMessage(channelMessage);
            }
            else if (message is ImuseMessage imuse)
            {
                HandleImuse(imuse);
            }
            else if (message is SysexMessage sysex)
            {
                HandleSysex(sysex);
            }
            else if (message is MetaMessage meta)
            {
                HandleMetaEvent(meta);
            }
        }

        private void HandleImuse(ImuseMessage message)
        {
            switch (message)
            {
                // Parts
                case ImuseAllocPart alloc:
                    parts.ExplicitAllocPart(this, alloc);
                    break;
                case ImuseDeallocPart dealloc:
                    parts.DeallocPart(dealloc.Channel);
                    break;
                case ImuseDeallocAllParts:
                    parts.DeallocAllParts(this);
                    break;

                // TODO: Marker meta messages for hooks
                // Hooks
                case ImuseHookJump jump:
                    jumpsExecuted.TryGetValue(jump, out int executedCount);
                    // We allow limiting the number of jumps performed for a given hook. This is useful for non-interactive recording.
                    if (executedCount < options.JumpLimit)
                    {
                        if (HookBlock.HandleJump(jump.Hook, jump.Chunk, jump.Beat, jump.Tick))
                        {
                            jumpsExecuted[jump] = executedCount + 1;
                        }
                    }
                    break;
                case ImuseV3HookJump jump:
                    jumpsExecuted.TryGetValue(jump, out int executed3Count);
                    // We allow limiting the number of jumps performed for a given hook. This is useful for non-interactive recording.
                    if (executed3Count < options.JumpLimit)
                    {
                        // TODO: This hasn't been checked, and probably isn't right (e.g. measure * 4). Just a quick test.
                        // Also needs to handle the Sustain property on the jump
                        if (HookBlock.HandleJump(jump.Hook, jump.Chunk, ((jump.Measure - 1) * 4) + jump.Beat, jump.Tick, jump.Sustain))
                        {
                            jumpsExecuted[jump] = executed3Count + 1;
                        }
                    }
                    break;
                case ImuseHookTranspose transpose:
                    HookBlock.HandleTranspose(transpose.Hook, transpose.Interval, transpose.Relative != 0);
                    break;
                case ImuseHookPartEnable partEnable:
                    HookBlock.HandlePartEnable(partEnable.Hook, partEnable.Channel, partEnable.Enabled != 0);
                    break;
                case ImuseHookPartVolume partVolume:
                    HookBlock.HandlePartVolume(partVolume.Hook, partVolume.Channel, partVolume.Volume);
                    break;
                case ImuseHookPartProgramChange partPgmCh:
                    HookBlock.HandlePartProgramChange(partPgmCh.Hook, partPgmCh.Channel, partPgmCh.Program);
                    break;
                case ImuseHookPartTranspose partTranspose:
                    HookBlock.HandlePartTranspose(partTranspose.Hook, partTranspose.Channel, partTranspose.Interval, partTranspose.Relative != 0);
                    break;

                case ImuseMarker marker:
                    queue.ProcessMarker(this, marker.Id);
                    break;
                case ImuseV3Marker marker:
                    queue.ProcessMarker(this, marker.Id);
                    break;

                // Loops
                case ImuseSetLoop setLoop:
                    // We allow limiting number of loops - this is useful for non-interactive recording
                    int count = Math.Min(setLoop.Count, options.LoopLimit);
                    sequencer.SetLoop(count, setLoop.StartBeat, setLoop.StartTick, setLoop.EndBeat, setLoop.EndTick);
                    break;
                case ImuseClearLoop:
                    sequencer.ClearLoop();
                    break;

                case ImuseStoredSetup setup:
                    driver.StoredSetup(setup.SetupNumber, setup.Setup);
                    break;

                default:
                    linkedParts.HandleImuseMessage(message);
                    break;
            }
        }

        private void HandleSysex(SysexMessage message)
        {
            driver.TransmitSysex(message, linkedParts);
        }

        private void HandleMetaEvent(MetaMessage message)
        {
            switch (message)
            {
                case SetTempoMessage tempo:
                    // TODO: Actually, the Sequencer should handle tempo changes (and combine them with Speed (percentage with 128 being 100%))
                    // - since tempo may differ between soundfiles playing at the same time.
                    driver.SetTempo(tempo);
                    break;
                case EndOfTrackMessage:
                    Stop();
                    break;
                default:
                    driver.TransmitMeta(message);
                    break;
            }
        }
    }
}
