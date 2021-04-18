using ImuseSequencer.Drivers;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public enum PlayerStatus
    {
        Off,
        On,
    }

    public class Player
    {
        private readonly Driver driver;
        private readonly PartsManager parts;
        private readonly Sequencer sequencer;
        private readonly HookBlock hookBlock;

        // List of parts currently used by this player
        private readonly PartsCollection linkedParts;

        public PlayerStatus Status { get; private set; }
        public int SoundId { get; private set; }
        public int Priority { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }
        public int EffectiveVolume => ((Volume + 1) * 127) >> 7; // TODO: "127" is actually system master volume

        public PartsCollection Parts => linkedParts;

        public Player(Driver driver, PartsManager parts, Sustainer sustainer)
        {
            this.driver = driver;
            this.parts = parts;
            linkedParts = new PartsCollection(driver);
            Status = PlayerStatus.Off;
            hookBlock = new HookBlock();
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
        public void Start(int id, MidiFile file)
        {
            Status = PlayerStatus.On;
            SoundId = id;
            Priority = file.ImuseHeader?.Priority ?? 0;
            Volume = file.ImuseHeader?.Volume ?? 127;
            Pan = file.ImuseHeader?.Pan ?? 0;
            Transpose = file.ImuseHeader?.Transpose ?? 0;
            Detune = file.ImuseHeader?.Detune ?? 0;

            hookBlock.Clear();

            sequencer.Start(file);
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

        public void SetPriority(int priority)
        {
            Priority = priority;
            linkedParts.SetPriority(Part.OmniChannel, priority);
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

        public bool SetTranspose(int transpose, bool relative)
        {
            if (transpose < -24 || transpose > 24)
            {
                return false;
            }

            if (relative)
            {
                Transpose = Math.Clamp(transpose + Transpose, -7, 7);
            }
            else
            {
                Transpose = transpose;
            }

            linkedParts.SetTranspose(Part.OmniChannel, Transpose, relative);
            return true;
        }

        public void SetDetune(int detune)
        {
            Detune = detune;
            linkedParts.SetDetune(Part.OmniChannel, detune);
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
                case ImuseAllocPart alloc:
                    parts.AllocPart(this, alloc);
                    break;
                case ImuseDeallocPart dealloc:
                    parts.DeallocPart(dealloc.Channel);
                    break;
                case ImuseDeallocAllParts:
                    parts.DeallocAllParts();
                    break;
                case ImuseSetLoop setLoop:
                    // TODO: For now, we're limiting loops to 3 - or we'll get e.g. 1000 generated while testing
                    int count = Math.Min(setLoop.Count, 2);
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
            driver.Sysex(message);
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
            }
        }
    }
}
