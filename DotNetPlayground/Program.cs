using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetPlayground
{
    class Program
    {
        static void Main(string[] args)
        {
            var game = new Game();
            game.Start();
        }
    }

    class Game
    {
        const int PLAYER_SERVER_SPEED = 1;
        const int PLAYER_CLIENT_SPEED = 1;
        const int BUFFER_SIZE = 20;

        readonly PlayerUpdate[] playerUpdates = new PlayerUpdate[BUFFER_SIZE];
        readonly Stopwatch stopwatch = new Stopwatch();

        PlayerUpdate updateFromServer;
        int idCounter;
        int playerClientPosition;
        int playerServerPosition;

        public void Start()
        {
            stopwatch.Start();

            Console.WriteLine("Starting game...\n\n");

            // player starts at position 0, and immediately initiates a move command.
            // Update is called every 1000ms.
            playerUpdates[idCounter % BUFFER_SIZE] = new PlayerUpdate
            {
                Id = idCounter,
                Position = playerClientPosition,
                State = "Moving"
            };

            idCounter++;

            while (true)
            {
                // first check if any corrections have been received from the server
                if (updateFromServer != null)
                {
                    Console.WriteLine($"Correction received from server. Client is currently at Update #{idCounter}, but server is sending correction for Update #{updateFromServer.Id}.\n");
                    Console.WriteLine($"After Update #{updateFromServer.Id}, Client thinks the Player's position is: {playerUpdates[updateFromServer.Id % BUFFER_SIZE].Position}, but Server thinks the Player's position is {updateFromServer.Position}.\n");

                    // First, update the incorrect PlayerUpdate record in the Client based on the correction received from the server
                    playerUpdates[updateFromServer.Id % BUFFER_SIZE].Position = updateFromServer.Position;
                    playerClientPosition = updateFromServer.Position;

                    // Now, recalculate every Update call between updateFromServer.Id to idCounter
                    for (var i = updateFromServer.Id; i < idCounter; i++)
                    {
                        var pastUpdate = playerUpdates[i % BUFFER_SIZE];
                        var moveDistance = pastUpdate.DeltaTime * PLAYER_CLIENT_SPEED;
                        playerClientPosition += moveDistance;

                        Console.WriteLine($"Reprocessing update on client... Id: {i}\n");
                        Console.WriteLine($"Distance traveled since last update: {moveDistance}\n");
                        Console.WriteLine($"Player's new position: {playerClientPosition}\n");
                    }

                    Console.WriteLine($"Done correcting Player's position. Current position: {playerClientPosition}\n\n");

                    updateFromServer = null;
                }

                // then check if enough time has elapsed to process another Update
                var elapsedMS = (int)stopwatch.ElapsedMilliseconds;
                if (elapsedMS > 1000)
                {
                    var moveDistance = elapsedMS * PLAYER_CLIENT_SPEED;
                    playerClientPosition += moveDistance;

                    Console.WriteLine($"Processing update on client... Id: {idCounter}\n");
                    Console.WriteLine($"Distance traveled since last update: {moveDistance}\n");
                    Console.WriteLine($"Player's new position: {playerClientPosition}\n\n");
                    
                    var newUpdate = new PlayerUpdate
                    {
                        Id = idCounter,
                        DeltaTime = elapsedMS,
                        Position = playerClientPosition,
                        State = "Moving"
                    };

                    // Simulate sending this update to the server by making an async call with a delay
                    Task.Run(() =>
                    {
                        ProcessUpdateOnServer(newUpdate);
                    }).ConfigureAwait(false);

                    playerUpdates[idCounter % BUFFER_SIZE] = newUpdate;

                    idCounter++;

                    stopwatch.Restart();
                }
            }
        }

        void ProcessUpdateOnServer(PlayerUpdate newUpdate)
        {
            // Simulate the delay between client and server by using Thread.Sleep
            Thread.Sleep(3500);

            Console.WriteLine("Calling Update from Server.\n");

            var moveDistance = newUpdate.DeltaTime * PLAYER_SERVER_SPEED;
            playerServerPosition += moveDistance;

            // if the client and server disagree on the player's position, the server returns
            // a correction to the client. this just correct the player's position,
            // but you imagine updating the player's health, animation state, etc.
            if (playerServerPosition != newUpdate.Position)
            {
                Console.WriteLine($"Server and Client disagree. PlayerPosition received from Client: {newUpdate.Position}, PlayerPosition calculated on Server: {playerServerPosition}.\n");
                Console.WriteLine("Sending correction to client.\n\n");

                updateFromServer = new PlayerUpdate
                {
                    Id = newUpdate.Id,
                    DeltaTime = newUpdate.DeltaTime,
                    Position = playerServerPosition,
                    State = "Moving"
                };
            }
            else
                Console.WriteLine("Client and Server agree. No correction necessary!\n\n");
        }
    }

    class PlayerUpdate
    {
        public int Id { get; set; }
        public int DeltaTime { get; set; }
        public int Position { get; set; }
        public string State { get; set; }
    }
}
