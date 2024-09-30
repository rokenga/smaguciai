using SaudomGaudom.Classes;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SaudomGaudom.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private Player currentPlayer;
        private Dictionary<Player, PlayerVisual> playerVisuals = new Dictionary<Player, PlayerVisual>();
        private Dictionary<Enemy, Rectangle> enemyVisuals = new Dictionary<Enemy, Rectangle>();
        private List<Obstacle> obstacles = new List<Obstacle>();
        private List<Enemy> enemies = new List<Enemy>();

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Create and show the ColorChoiceForm as a pop-up
            ColorChoiceForm colorChoiceForm = new ColorChoiceForm();
            colorChoiceForm.ShowDialog();
            string selectedColor = colorChoiceForm.SelectedColor;

            client = new TcpClient("localhost", 12345);
            stream = client.GetStream();

            byte[] colorBytes = Encoding.UTF8.GetBytes(selectedColor);
            stream.Write(colorBytes, 0, colorBytes.Length);
            stream.Flush();

            var receiveThread = new System.Threading.Thread(ReceivePlayerData);
            receiveThread.Start();
        }
        public String getFirstInstanceUsingSubString(String input, string something)
        {
            int index = input.Contains(something) ? input.IndexOf(something) + something.Length - 1 : 0;
            return input.Substring(0, index);
        }
        public string GetStringAfterFirstSpace(string input, string something)
        {
            int index = input.Contains(something) ? input.IndexOf(something) + something.Length : 0;
            return input.Substring(index);
        }
        private void ReceivePlayerData()
        {
            bool initialized = false;
            List<Player> playerInfoList = new List<Player>();

            while (true)
            {
                string message = ReceiveString(stream);
                if (message.Contains("ENEMY_POSITIONS;"))
                {
                    // Extract enemy positions from the message
                    string[] parts = message.Split(';');
                    if (parts.Length >= 2)
                    {
                        string[] enemyInfo = parts[1].Split(',');
                        foreach (var enemyData in enemyInfo)
                        {
                            string[] enemyDetails = enemyData.Split(':');
                            if (enemyDetails.Length == 4)
                            {
                                int enemyId = int.Parse(enemyDetails[0]);
                                string enemyColor = enemyDetails[1];
                                double enemyX = double.Parse(enemyDetails[2]);
                                double enemyY = double.Parse(enemyDetails[3]);
                                Enemy enemy = new Enemy(enemyId, enemyColor);
                                enemy.SetCurrentPosition(enemyX, enemyY);
                                if (!enemies.Contains(enemy))
                                {
                                    Debug.WriteLine("Enemy spawned");
                                    enemies.Add(enemy);
                                    Dispatcher.Invoke(() =>
                                    {
                                        Rectangle enemyRect = new Rectangle
                                        {
                                            Width = 20,
                                            Height = 20,
                                            Fill = Brushes.Red, // Set the enemy's color
                                        };
                                        enemyVisuals[enemy] = enemyRect;
                                        Canvas.SetLeft(enemyRect, enemyX);
                                        Canvas.SetTop(enemyRect, enemyY);

                                        // Add the enemy to the canvas or container where you want to display them
                                        EnemyContainer.Children.Add(enemyRect);
                                    });
                                }
                                else
                                {
                                    enemies.Find(n => n.Equals(enemy)).SetCurrentPosition(enemyX, enemyY);
                                }
                            }
                        }
                    }
                }
                if (message.StartsWith("ObstacleData:"))
                {
                    string obstaclemessage = getFirstInstanceUsingSubString(message, "I");
                    if (obstaclemessage == "")
                    {
                        obstaclemessage = message;
                    }
                    obstaclemessage = obstaclemessage.Substring("ObstacleData:".Length);
                    string[] obstaclemessages = obstaclemessage.Split(',');
                    foreach (string obs in obstaclemessages)
                    {
                        string[] parts = obs.Split(':');
                        if (parts.Length == 4)
                        {
                            double width = double.Parse(parts[0]);
                            double height = double.Parse(parts[1]);
                            double posX = double.Parse(parts[2]);
                            double posY = double.Parse(parts[3]);

                            Obstacle obstacle = new Obstacle(width, height, posX, posY);
                            obstacles.Add(obstacle);
                        }
                    }
                    message = message.Remove(0, "ObstacleData:".Length + obstaclemessage.Length - 1);
                }
                if (message.Contains("INIT:") && !initialized)
                {
                    string initMessage = getFirstInstanceUsingSubString(message, " ");
                    // Parse and process the initialization message
                    string[] parts = initMessage.Split(':');
                    if (parts.Length == 6)
                    {
                        int playerId = int.Parse(parts[1]);
                        string playerName = parts[2];
                        string playerColor = parts[3];
                        int x = int.Parse(parts[4]);
                        int y = int.Parse(parts[5]);

                        // Create a Player object or update the current player's information
                        currentPlayer = new Player(playerId, playerName, playerColor);
                        currentPlayer.SetCurrentPosition(x, y);

                        playerInfoList.Add(currentPlayer);

                        initialized = true;
                    }
                }
                if (message.Contains("PLAYER_LIST;") && initialized)
                {
                    string playerList = GetStringAfterFirstSpace(message, " ").Split(';')[1];
                    // Assuming the player list format is "PLAYER_LIST;ID:Name:Color,ID:Name:Color,..."

                    // Split the player list into individual player entries

                    string[] playerEntries = playerList.Split(',');

                    foreach (string playerEntry in playerEntries)
                    {
                        //Split each player entry into ID, Name, and Color parts
                        string[] parts = playerEntry.Split(':');

                        if (parts.Length == 5)
                        {
                            int playerId = int.Parse(parts[0]);
                            string playerName = parts[1];
                            string playerColor = parts[2];
                            int x = int.Parse(parts[3]);
                            int y = int.Parse(parts[4]);

                            //Create a PlayerInfo object to store player information
                            Player playerInfo = new Player(playerId, playerName, playerColor);
                            playerInfo.SetCurrentPosition(x, y);
                            playerInfoList.Add(playerInfo);
                        }
                    }
                }
                UpdateClientView(playerInfoList);

                // Update the client's view to display the players

            }
        }
        private void UpdateClientView(List<Player> playerInfoList)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (Player playerInfo in playerInfoList)
                {
                    if (playerVisuals.ContainsKey(playerInfo))
                    {
                        PlayerVisual playerVisual = playerVisuals[playerInfo];
                        double x = Canvas.GetLeft(playerVisual);
                        Canvas.SetLeft(playerVisual, playerInfo.GetCurrentX());
                        Canvas.SetTop(playerVisual, playerInfo.GetCurrentY());
                    }
                    else
                    {
                        PlayerVisual playerVisual = new PlayerVisual();
                        ColorConverter converter = new ColorConverter();
                        Color playerColor = (Color)ColorConverter.ConvertFromString(playerInfo.GetColor());
                        SolidColorBrush solidColorBrush = new SolidColorBrush(playerColor);
                        playerVisual.PlayerColor = solidColorBrush;
                        playerVisual.UpdateColor(solidColorBrush);
                        Canvas.SetLeft(playerVisual, playerInfo.GetCurrentX());
                        Canvas.SetTop(playerVisual, playerInfo.GetCurrentY());
                        playerVisuals[playerInfo] = playerVisual;
                        playersContainer.Items.Add(playerVisual);
                    }
                }
                foreach (Enemy enemy in enemies)
                {
                    Rectangle enemyRect = enemyVisuals[enemy];
                    Canvas.SetLeft(enemyRect, enemy.GetCurrentX());
                    Canvas.SetTop(enemyRect, enemy.GetCurrentY());
                }
            });
        }
        private string ReceiveString(NetworkStream stream)
        {
            byte[] buffer = new byte[1024]; // Adjust the buffer size as needed
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            Canvas gameMapCanvas = new Canvas();
            gameMapCanvas.Name = "GameMap";
            gameMapCanvas.Background = Brushes.Gray;

            // Create a Rectangle (obstacle)
            foreach (Obstacle obs in obstacles)
            {
                Rectangle obstacleRect = new Rectangle();

                obstacleRect.Width = obs.Width;
                obstacleRect.Height = obs.Height;
                obstacleRect.Fill = Brushes.LightGray;
                Canvas.SetLeft(obstacleRect, obs.PositionX);
                Canvas.SetTop(obstacleRect, obs.PositionY);

                // Add the Rectangle to the Canvas
                gameMapCanvas.Children.Add(obstacleRect);
            }
            // Replace the existing Canvas with the new one
            playersContainer.Items.Add(gameMapCanvas);
        }

        private int playerDirectionX = 0;
        private int playerDirectionY = 0;
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            int deltaX = 0;
            int deltaY = 0;

            // Handle arrow key presses here and update the player's position
            // based on the arrow key input.
            if (e.Key == Key.Left)
            {
                deltaX = -1;
                playerDirectionX = deltaX; // Store the player's direction
                playerDirectionY = 0;
            }
            else if (e.Key == Key.Right)
            {
                deltaX = 1;
                playerDirectionX = deltaX; // Store the player's direction
                playerDirectionY = 0;
            }
            else if (e.Key == Key.Up)
            {
                deltaY = -1;
                playerDirectionY = deltaY; // Store the player's direction
                playerDirectionX = 0;
            }
            else if (e.Key == Key.Down)
            {
                deltaY = 1;
                playerDirectionY = deltaY; // Store the player's direction
                playerDirectionX = 0;
            }

            if (e.Key == Key.Space)
            {
                // Use the stored direction for shooting
                Shoot(playerDirectionX, playerDirectionY);

            }

            double newX = currentPlayer.GetCurrentX() + deltaX * 10;
            double newY = currentPlayer.GetCurrentY() + deltaY * 10;

            bool overlap = false;
            // TO DO: jei overlapina bet yra kelio iki to obstacle padaryt, kad galetu nueit, dbr tng
            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.WouldOverlap(newX, newY, 50, 50))
                {
                    overlap = true;
                    break;
                }
            }
            if (!overlap)
            {
                UpdatePlayer(playerVisuals[currentPlayer], deltaX, deltaY);

                string movementUpdateMessage = $"MOVE:{currentPlayer.GetId()}:{newX}:{newY}";

                // Send the movement update message to the server using the client's network stream
                byte[] updateData = Encoding.UTF8.GetBytes(movementUpdateMessage);
                stream.Write(updateData, 0, updateData.Length);
                stream.Flush();
            }
        }

        private void Shoot(int deltaX, int deltaY)
        {
            CreateShot(playerVisuals[currentPlayer], deltaX, deltaY);
        }

        private void CreateShot(PlayerVisual playerVisual, double directionX, double directionY)
        {
            // Create a new shot visual element (e.g., a bullet or projectile)
            Ellipse shotVisual = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Red // You can customize the shot appearance
            };

            // Set the initial position of the shot to match the player's position
            double playerX = Canvas.GetLeft(playerVisual);
            double playerY = Canvas.GetTop(playerVisual);
            Canvas.SetLeft(shotVisual, playerX);
            Canvas.SetTop(shotVisual, playerY);

            // Add the shot to the ShotContainer (Canvas)
            ShotContainer.Children.Add(shotVisual);

            // Define the speed of the shot (you can adjust this value)
            double shotSpeed = 5;

            // Update the shot's position based on the direction and speed
            CompositionTarget.Rendering += (sender, e) =>
            {
                double currentX = Canvas.GetLeft(shotVisual);
                double currentY = Canvas.GetTop(shotVisual);

                double newX = currentX + directionX * shotSpeed;
                double newY = currentY + directionY * shotSpeed;

                Canvas.SetLeft(shotVisual, newX);
                Canvas.SetTop(shotVisual, newY);

                // Example: Remove the shot if it goes out of bounds
                if (newX < 0 || newX >= ShotContainer.ActualWidth || newY < 0 || newY >= ShotContainer.ActualHeight)
                {
                    ShotContainer.Children.Remove(shotVisual);
                }

                //TODO: remove jeigu i siena pataiko
                //TODO: skaiciuot taskus jei i enemy pataiko
            };
        }

        private void UpdatePlayer(PlayerVisual playerVisual, int deltaX, int deltaY)
        {
            double currentX = Canvas.GetLeft(playerVisual);
            double currentY = Canvas.GetTop(playerVisual);

            // Calculate the new position
            double newX = currentX + deltaX * 10;
            double newY = currentY + deltaY * 10;

            // Set the new position
            Canvas.SetLeft(playerVisual, newX);
            Canvas.SetTop(playerVisual, newY);

            currentPlayer.SetCurrentPosition(newX, newY);
        }
        private string UpdatePlayerPosition(PlayerVisual playerVisual)
        {
            double currentX = Canvas.GetLeft(playerVisual);
            double currentY = Canvas.GetTop(playerVisual);

            return $"MOVE:{currentPlayer.GetId()}:{currentX}:{currentY}";
        }
    }
}