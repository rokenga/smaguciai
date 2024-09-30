 using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Text;
using System;
using SaudomGaudom.Classes;

class Server
{
    private List<Player> players = new List<Player>();
    private TcpListener listener;
    private Dictionary<int, NetworkStream> playerStreams = new Dictionary<int, NetworkStream>();
    private List<Obstacle> obstacles = new List<Obstacle>();
    private List<Enemy> enemies = new List<Enemy>();
    private int enemySpawnIntervalInSeconds = 10;
    private int enemyUpdateIntervalInMilliseconds = 203;

    public Server()
    {
        listener = new TcpListener(IPAddress.Any, 12345);
        StartEnemySpawnTimer();
        StartEnemyUpdateTimer();
    }
    private void StartEnemySpawnTimer()
    {
        Timer enemySpawnTimer = new Timer(EnemySpawnCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(enemySpawnIntervalInSeconds));
    }

    private void EnemySpawnCallback(object state)
    {
        Random random = new Random();
        Enemy enemy = new Enemy(enemies.Count + 1, "Red", 2);
        double spawnX = random.Next(0, 1936);
        double spawnY = random.Next(0, 1056);
        enemy.SetCurrentPosition(spawnX, spawnY);
        lock (enemies)
        {
            enemies.Add(enemy);
        }

        Console.WriteLine("Enemy spawned");
        BroadcastEnemyPositions();
    }

    public void Start()
    {
        listener.Start();
        GenerateObstacles();
        Console.WriteLine("Server started. Waiting for connections...");
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected.");

            Thread clientThread = new Thread(() => HandleClient(client));
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        string tempColor = ReceiveString(stream);

        int playerId;
        lock (players)
        {
            playerId = players.Count + 1;
        }

        Player newPlayer = new Player(playerId, "a", tempColor);

        Random random = new Random();
        newPlayer.SetCurrentPosition(0, 0);
        players.Add(newPlayer);
        playerStreams.Add(playerId, stream);

        string obstacleData = string.Join(",", obstacles.Select(obstacle => obstacle.ToString()));
        string fullObstacleData = $"ObstacleData:{obstacleData}";
        Console.WriteLine(fullObstacleData);
        byte[] obstacleBytes = Encoding.UTF8.GetBytes(fullObstacleData);
        stream.Write(obstacleBytes, 0, obstacleBytes.Length);

        string initializationMessage = $"INIT:{newPlayer.GetId()}:{newPlayer.GetName()}:{newPlayer.GetColor()}:{newPlayer.GetCurrentX()}:{newPlayer.GetCurrentY()} ";
        Console.Write(initializationMessage);
        byte[] initBytes = Encoding.UTF8.GetBytes(initializationMessage);
        stream.Write(initBytes, 0, initBytes.Length);
        stream.Flush();

        BroadcastPlayerList();

        while (true)
        {
            string message = ReceiveString(stream);

            if (message.StartsWith("MOVE:"))
            {
                HandleMovementUpdate(message);
            }
        }
    }

    private void GenerateObstacles()
    {
        Random random = new Random();
        for (int i = 0; i < random.Next(10, 14); i++)
        {
            int widthTemp = random.Next(50, 300);
            int heightTemp = (widthTemp < 150) ? random.Next(200, 300) : random.Next(50, 100);

            // TO DO: atsisakau daryti packing algoritma

            int xtemp = random.Next(0, 1936 - widthTemp);
            int ytemp = random.Next(0, 1056 - heightTemp);

            Obstacle obstacle = new Obstacle(widthTemp, heightTemp, xtemp, ytemp);
            obstacles.Add(obstacle);
        }
    }
    private void HandleMovementUpdate(string message)
    {
        string[] parts = message.Split(':');
        if (parts.Length == 4 && parts[0] == "MOVE")
        {
            Console.WriteLine(message);
            int playerId = int.Parse(parts[1]);
            int deltaX = int.Parse(parts[2]);
            int deltaY = int.Parse(parts[3]);

            Player playerToUpdate = players.FirstOrDefault(p => p.GetId() == playerId);
            if (playerToUpdate != null)
            {
                playerToUpdate.SetCurrentPosition(deltaX, deltaY);
            }

            BroadcastPlayerList();
        }
    }

    private void BroadcastPlayerList()
    {
        foreach (Player player in players)
        {
            string playerList = "PLAYER_LIST;" + string.Join(",", players
                .Where(p => p.GetId() != player.GetId())
                .Select(p => $"{p.GetId()}:{p.GetName()}:{p.GetColor()}:{p.GetCurrentX()}:{p.GetCurrentY()}"));

            if (playerList.Length > "PLAYER_LIST;".Length) { Console.WriteLine($"{playerList}"); }

            NetworkStream stream = GetStreamForPlayer(player);
            SendString(stream, playerList);
        }
    }

    private NetworkStream GetStreamForPlayer(Player player)
    {
        if (playerStreams.TryGetValue(player.GetId(), out NetworkStream stream))
        {
            return stream;
        }
        else
        {
            throw new InvalidOperationException($"Stream for player {player.GetId()} not found.");
        }
    }

    private void SendString(NetworkStream stream, string data)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(data);
        stream.Write(bytes, 0, bytes.Length);
    }

    private string ReceiveString(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    private void UpdateEnemyPositions()
    {
        lock (enemies)
        {
            foreach (var enemy in enemies)
            {
                Player closestPlayer = FindClosestPlayer(enemy);

                if (closestPlayer != null)
                {
                    double directionX = closestPlayer.GetCurrentX() - enemy.GetCurrentX();
                    double directionY = closestPlayer.GetCurrentY() - enemy.GetCurrentY();

                    double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                    if (length > 0)
                    {
                        directionX /= length;
                        directionY /= length;
                    }

                    double enemySpeed = enemy.GetSpeed();

                    double newX = enemy.GetCurrentX() + directionX * enemySpeed;
                    double newY = enemy.GetCurrentY() + directionY * enemySpeed;

                    enemy.SetCurrentPosition(newX, newY);
                }
            }
        }
    }

    private Player FindClosestPlayer(Enemy enemy)
    {
        Player closestPlayer = null;
        double closestDistance = double.MaxValue;

        foreach (var player in players)
        {
            double distance = Math.Sqrt(
                Math.Pow(player.GetCurrentX() - enemy.GetCurrentX(), 2) +
                Math.Pow(player.GetCurrentY() - enemy.GetCurrentY(), 2));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }

        return closestPlayer;
    }

    private void StartEnemyUpdateTimer()
    {
        Timer enemyUpdateTimer = new Timer(EnemyUpdateCallback, null, TimeSpan.FromSeconds(31), TimeSpan.FromMilliseconds(enemyUpdateIntervalInMilliseconds));
    }

    private void EnemyUpdateCallback(object state)
    {
        UpdateEnemyPositions();
        BroadcastEnemyPositions();
    }

    private void BroadcastEnemyPositions()
    {
        string message;
        lock (enemies)
        {
            string enemyPositions = string.Join(",", enemies.Select(enemy =>
                $"{enemy.GetId()}:{enemy.GetColor()}:{enemy.GetCurrentX()}:{enemy.GetCurrentY()}"));
            message = $"ENEMY_POSITIONS;{enemyPositions}";
        }
        foreach (var stream in playerStreams.Values)
        {
            SendString(stream, message);
        }
    }

}

class Program
{
    static void Main()
    {
        Server server = new Server();
        server.Start();
    }
}
