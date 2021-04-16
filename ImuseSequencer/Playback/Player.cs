using ImuseSequencer.Drivers;
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
        private readonly Sequencer sequencer;
        private readonly HookBlock hookBlock;

        private readonly PartsCollection linkedParts;

        public PlayerStatus Status { get; private set; }
        public int SoundId { get; private set; }
        public int Priority { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }
        public int EffectiveVolume => ((Volume + 1) * 127) >> 7; // TODO: "127" is actually system master volume

        public Player(Driver driver)
        {
            linkedParts = new PartsCollection(driver);
            Status = PlayerStatus.Off;
            hookBlock = new HookBlock();
            sequencer = new Sequencer();
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
            linkedParts.SetPriority(priority, Part.OmniChannel);
        }

        public bool SetVolume(int volume)
        {
            if (volume > 127)
            {
                return false;
            }
            Volume = volume;
            linkedParts.SetVolume(volume, Part.OmniChannel);
            return true;
        }

        public void SetPan(int pan)
        {
            Pan = pan;
            linkedParts.SetPan(pan, Part.OmniChannel);
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

            linkedParts.SetTranspose(Transpose, relative, Part.OmniChannel);
            return true;
        }

        public void SetDetune(int detune)
        {
            Detune = detune;
            linkedParts.SetDetune(detune, Part.OmniChannel);
        }

        // UpdateMasterVolume not needed (probably) - we use property evaluation for effective volume

    }
}
