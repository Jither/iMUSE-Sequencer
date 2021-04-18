using ImuseSequencer.Drivers;
using Jither.Logging;
using System.Collections.Generic;

namespace ImuseSequencer.Playback
{
    public class PlayerManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PlayerManager));
        private const int playerCount = 8;

        private readonly FileManager files;
        private readonly PartsManager parts;
        private readonly List<Player> players = new();

        public PlayerManager(FileManager files, PartsManager parts, Sustainer sustainer, Driver driver)
        {
            this.files = files;
            this.parts = parts;

            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new Player(driver, parts, sustainer));
            }
        }

        /// <summary>
        /// Retrieves sound with given ID, and tries to select an available player to play it, based on the sound file's priority.
        /// If a player is selected which is already playing, it will stop that player and restart it with the new sound.
        /// </summary>
        /// <returns><c>true</c> if a player was found and the sound has started. Otherwise <c>false</c>.</returns>
        public bool StartSound(int id)
        {
            var file = files.Get(id);
            var player = SelectPlayer(file.ImuseHeader?.Priority ?? 0);
            if (player == null)
            {
                return false;
            }

            player.Stop();

            player.Start(id, file);
            return true;
        }

        // TODO: Temporary quick-hack
        public bool Tick()
        {
            bool done = true;
            foreach (var player in players)
            {
                if (!player.Tick())
                {
                    done = false;
                }
            }
            return done;
        }

        /// <summary>
        /// Stops all instances of the given sound.
        /// </summary>
        /// <returns><c>true</c> if the sound was found. Otherwise <c>false</c>.</returns>
        public bool StopSound(int id)
        {
            bool found = false;
            foreach (var player in players)
            {
                if (player.Status == PlayerStatus.On && player.SoundId == id)
                {
                    player.Stop();
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// Stops all players and clears queue. 
        /// </summary>
        public void StopAllSounds()
        {
            // TODO: Clear queue
            foreach (var player in players)
            {
                player.Stop();
            }
        }

        public PlayerStatus GetStatus(int soundId)
        {
            foreach (var player in players)
            {
                if (player.Status == PlayerStatus.On && player.SoundId == soundId)
                {
                    return PlayerStatus.On;
                }
            }

            // TODO: Get from queue
            return PlayerStatus.Off;
        }

        /// <summary>
        /// Finds player that's not currently playing - or, if all players are busy,
        /// steals the weakest one with lower priority than the given priority.
        /// </summary>
        private Player SelectPlayer(int priority)
        {
            int lowestPriority = priority;
            Player weakestPlayer = null;

            foreach (var player in players)
            {
                if (player.Status == PlayerStatus.Off)
                {
                    return player;
                }

                if (player.Status == PlayerStatus.On)
                {
                    if (player.Priority <= lowestPriority)
                    {
                        lowestPriority = player.Priority;
                        weakestPlayer = player;
                    }
                }
            }

            logger.DebugWarning("No spare players... Selecting the one with lowest priority");

            return weakestPlayer;
        }
    }
}
