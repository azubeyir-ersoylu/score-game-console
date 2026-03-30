using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SkorOyunu
{
    // --- LOGLAMA SINIFI ---
    public static class Logger
    {
        private const string LogFile = "oyun_log.txt";
        private static StreamWriter writer;

        public static void Init()
        {
            writer = new StreamWriter(LogFile, append: false, System.Text.Encoding.UTF8)
            {
                AutoFlush = true
            };
            writer.WriteLine("--- YENI OYUN BASLADI ---");
        }

        public static void Log(string action, string details)
        {
            writer?.WriteLine($"{action} -> {details}");
        }

        public static void Close()
        {
            writer?.Close();
        }
    }

    // --- DÜŞEN NESNELER VE OYUNCU İÇİN SINIF ---
    public class GameObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Symbol { get; set; }
        public ConsoleColor Color { get; set; }

        public GameObject(int x, int y, char symbol, ConsoleColor color)
        {
            X = x;
            Y = y;
            Symbol = symbol;
            Color = color;
        }

        public void Draw()
        {
            if (X >= 0 && X < Console.WindowWidth && Y >= 0 && Y < Console.WindowHeight)
            {
                Console.SetCursorPosition(X, Y);
                Console.ForegroundColor = Color;
                Console.Write(Symbol);
            }
        }

        public void Erase()
        {
            if (X >= 0 && X < Console.WindowWidth && Y >= 0 && Y < Console.WindowHeight)
            {
                Console.SetCursorPosition(X, Y);
                Console.Write(' ');
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.Clear();

            Logger.Init();

            int targetScore = 15;
            int timeLimit = 30;
            int score = 0;

            int width = Console.WindowWidth;
            int height = Console.WindowHeight;

            Logger.Log("SYSTEM", $"Oyun baslatildi. HedefSkor={targetScore} Sure={timeLimit}");

            GameObject player = new GameObject(width / 2, height - 2, '#', ConsoleColor.Green);
            List<GameObject> items = new List<GameObject>();
            Random rnd = new Random();

            Stopwatch timer = new Stopwatch();
            timer.Start();

            long lastUpdateTick = timer.ElapsedMilliseconds;
            int itemDropDelay = 150;
            bool isRunning = true;
            string endReason = "";

            while (isRunning)
            {
                long currentTick = timer.ElapsedMilliseconds;
                int elapsedSeconds = (int)(currentTick / 1000);
                int remainingTime = timeLimit - elapsedSeconds;

                // 1. OYUN BİTİŞ KONTROLÜ
                if (remainingTime <= 0)
                {
                    endReason = "SureBitti";
                    isRunning = false;
                    break;
                }

                if (score >= targetScore)
                {
                    endReason = "SkoraUlasildi";
                    isRunning = false;
                    break;
                }

                // 2. KLAVYE GİRDİSİ
                while (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    int oldX = player.X;

                    if (keyInfo.Key == ConsoleKey.LeftArrow && player.X > 0)
                    {
                        player.X--;
                        Logger.Log("INPUT", $"key=LeftArrow playerX={player.X} playerY={player.Y}");
                        Logger.Log("PLAYER_MOVE", $"oldX={oldX} newX={player.X} playerY={player.Y}");
                    }
                    else if (keyInfo.Key == ConsoleKey.RightArrow && player.X < width - 1)
                    {
                        player.X++;
                        Logger.Log("INPUT", $"key=RightArrow playerX={player.X} playerY={player.Y}");
                        Logger.Log("PLAYER_MOVE", $"oldX={oldX} newX={player.X} playerY={player.Y}");
                    }
                    else if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        endReason = "KullaniciCikti";
                        Logger.Log("INPUT", $"key=Escape playerX={player.X} playerY={player.Y}");
                        isRunning = false;
                    }

                    if (oldX != player.X)
                    {
                        Console.SetCursorPosition(oldX, player.Y);
                        Console.Write(' ');
                    }
                }

                if (!isRunning)
                    break;

                // 3. NESNE OLUŞTURMA + HAREKET + ÇARPIŞMA
                if (currentTick - lastUpdateTick > itemDropDelay)
                {
                    // Yeni nesne üret
                    if (rnd.Next(0, 100) < 20)
                    {
                        int spawnX = rnd.Next(0, width);
                        char symbol = rnd.Next(0, 2) == 0 ? '*' : 'O';
                        ConsoleColor color = symbol == '*' ? ConsoleColor.Yellow : ConsoleColor.Cyan;

                        items.Add(new GameObject(spawnX, 1, symbol, color));
                        Logger.Log("UPDATE", $"itemSpawned char={symbol} x={spawnX} y=1");
                    }

                    // Nesneleri aşağı indir
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        GameObject item = items[i];
                        int oldY = item.Y;

                        item.Erase();
                        item.Y++;

                        Logger.Log("OBJECT_MOVE", $"char={item.Symbol} oldY={oldY} newY={item.Y} x={item.X}");
                        Logger.Log("COLLISION_CHECK", $"itemX={item.X} itemY={item.Y} playerX={player.X} playerY={player.Y}");

                        if (item.X == player.X && item.Y == player.Y)
                        {
                            score++;
                            Logger.Log("COLLISION", $"score={score} caughtChar={item.Symbol}");
                            Logger.Log("SCORE", $"newScore={score}");
                            items.RemoveAt(i);
                        }
                        else if (item.Y >= height)
                        {
                            items.RemoveAt(i);
                        }
                    }

                    lastUpdateTick = currentTick;
                }

                // 4. ÇİZİM
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($" Skor: {score}/{targetScore} | Kalan Sure: {Math.Max(0, remainingTime)}s ".PadRight(width));

                foreach (var item in items)
                {
                    item.Draw();
                }

                player.Draw();

                Thread.Sleep(15);
            }

            Logger.Log("GAME_OVER", $"reason={endReason} finalScore={score}");
            Logger.Close();

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;

            string endMsg = $"OYUN BITTI! Skorunuz: {score}";
            string exitMsg = "Cikmak icin herhangi bir tusa basin...";

            int widthNow = Console.WindowWidth;
            int heightNow = Console.WindowHeight;

            Console.SetCursorPosition(Math.Max(0, (widthNow - endMsg.Length) / 2), heightNow / 2);
            Console.WriteLine(endMsg);

            Console.SetCursorPosition(Math.Max(0, (widthNow - exitMsg.Length) / 2), (heightNow / 2) + 1);
            Console.WriteLine(exitMsg);

            while (Console.KeyAvailable)
                Console.ReadKey(true);

            Console.ReadKey(true);
        }
    }
}