using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Imuse.Messages;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse
{
    public enum PlayerStatus
    {
        Off,
        On,
    }

    /// <summary>
    /// Players manage the playback of a single sound (MIDI) file.
    /// </summary>
    public class Player
    {
        private readonly Driver driver;
        private readonly PartsManager parts;
        private readonly Sequencer sequencer;
        private SoundFile file;
        public HookBlock HookBlock { get; }

        // List of parts currently used by this player
        private readonly PartsCollection linkedParts;
        private readonly ImuseOptions options;

        // TODO: Temporary test measure:
        private readonly Dictionary<ImuseHookJump, int> jumpsExecuted = new();

        public PlayerStatus Status { get; private set; }
        public int SoundId { get; private set; }
        public int Priority { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }
        public int EffectiveVolume => ((Volume + 1) * 127) >> 7; // TODO: "127" is actually system master volume

        internal PartsCollection Parts => linkedParts;
        internal Sequencer Sequencer => sequencer;

        public Player(Driver driver, PartsManager parts, Sustainer sustainer, ImuseOptions options)
        {
            this.driver = driver;
            this.parts = parts;
            this.options = options;
            linkedParts = new PartsCollection(driver);
            Status = PlayerStatus.Off;
            HookBlock = new HookBlock(this);
            sequencer = new Sequencer(this, sustainer);
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

        // AKA DeallocAllParts
        public void UnlinkAllParts()
        {
            for (int i = linkedParts.Count - 1; i >= 0; i--)
            {
                UnlinkPart(linkedParts[i]);
            }
        }

        /// <summary>
        /// Starts playback of sound. 
        /// </summary>
        /// <returns><c>true</c> if playback successfully started. Otherwise <c>false</c>.</returns>
        public void Start(int id, SoundFile file)
        {
            this.file = file;

            Status = PlayerStatus.On;
            SoundId = id;
            Priority = file.ImuseHeader?.Priority ?? 0;
            Volume = file.ImuseHeader?.Volume ?? 127;
            Pan = file.ImuseHeader?.Pan ?? 0;
            Transpose = file.ImuseHeader?.Transpose ?? 0;
            Detune = file.ImuseHeader?.Detune ?? 0;

            HookBlock.Clear();

            sequencer.Start(file.Midi);
        }

        // Temporary
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
            UnlinkAllParts();
            Status = PlayerStatus.Off;
        }

        // TODO: Does anything actually need these "SetX" methods? (other than SetTranspose, used by hooks)
        public void SetPriority(int priority)
        {
            Priority = priority;
            linkedParts.SetPriority();
        }

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

        public void SetPan(int pan)
        {
            Pan = pan;
            linkedParts.SetPan();
        }

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

        /// <summary>
        /// Handles events from sequencer.
        /// </summary>
        public void HandleEvent(MidiMessage message)
        {
            if (message is ChannelMessage channelMessage)
            {
                linkedParts.HandleEvent(channelMessage);
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
                    parts.AllocPart(this, alloc);
                    break;
                case ImuseDeallocPart dealloc:
                    parts.DeallocPart(dealloc.Channel);
                    break;
                case ImuseDeallocAllParts:
                    parts.DeallocAllParts();
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
                case ImuseHookTranspose transpose:
                    HookBlock.HandleTranspose(transpose.Hook, transpose.Interval, transpose.Relative != 0);
                    break;
                case ImuseHookPartEnable partEnable:
                    HookBlock.HandlePartEnable(partEnable.Hook, partEnable.Channel, partEnable.Enabled != 0);
                    break;
                case ImuseHookPartVol partVolume:
                    HookBlock.HandlePartVolume(partVolume.Hook, partVolume.Channel, partVolume.Volume);
                    break;
                case ImuseHookPartPgmch partPgmCh:
                    HookBlock.HandlePartProgramChange(partPgmCh.Hook, partPgmCh.Channel, partPgmCh.Program);
                    break;
                case ImuseHookPartTranspose partTranspose:
                    HookBlock.HandlePartTranspose(partTranspose.Hook, partTranspose.Channel, partTranspose.Interval, partTranspose.Relative != 0);
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
                default:
                    linkedParts.HandleEvent(message);
                    break;
            }
        }

        private void HandleSysex(SysexMessage message)
        {
            // TODO: Full handling
            driver.TransmitSysex(message);
        }

        private void HandleMetaEvent(MetaMessage message)
        {
            switch (message)
            {
                case SetTempoMessage tempo:
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

        public InteractivityInfo GetInteractivityInfo()
        {
            return file?.GetInteractivityInfo();
        }
    }
}
