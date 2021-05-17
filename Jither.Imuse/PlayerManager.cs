using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Logging;
using System.Collections.Generic;

namespace Jither.Imuse
{
    /// <summary>
    /// Manages all the players of the ImuseEngine.
    /// </summary>
    public class PlayerManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PlayerManager));
        private const int playerCount = 8;

        private readonly FileManager files;
        private readonly PartManager parts;
        private readonly List<Player> players = new();

        public PlayerManager(FileManager files, PartManager parts, Sustainer sustainer, Driver driver, ImuseQueue queue, ImuseOptions options)
        {
            this.files = files;
            this.parts = parts;

            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new Player(i, driver, parts, sustainer, queue, options));
            }
        }

        public Player GetPlayerBySound(int soundId)
        {
            foreach (var player in players)
            {
                if (player.Status == PlayerStatus.On && player.SoundId == soundId)
                {
                    return player;
                }
            }
            return null;
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

        /// <summary>
        /// Renders (to the Driver) the next tick on every Player.
        /// </summary>
        /// <returns>
        /// <c>false</c> if all players have reached EndOfTrack. Otherwise <c>true</c> (active).
        /// </returns>
        public bool Tick()
        {
            int playersActive = 0;
            foreach (var player in players)
            {
                // If any single player is not done (EndOfTrack reached), then WE're not done.
                if (player.Tick())
                {
                    playersActive++;
                }
            }
            return playersActive > 0;
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
            if (GetPlayerBySound(soundId) != null)
            {
                return PlayerStatus.On;
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
