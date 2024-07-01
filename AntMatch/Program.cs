using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

class Program
{
    static bool raceOngoing;
    static int raceLength = 100;
    static List<Ant> ants = new List<Ant>();
    static CancellationTokenSource cts = new CancellationTokenSource();
    static string logFilePath = "race_log.txt";
    static object lockObject = new object();

    static void Main(string[] args)
    {
        while (true)
        {
            InitializeRace();
            StartRace();

            Console.WriteLine("Do you want to start another race? (yes/no)");
            string response = Console.ReadLine();
            if (response.ToLower() != "yes")
                break;
        }
    }

    // متد برای مقداردهی اولیه مسابقه
    static void InitializeRace()
    {
        ants.Clear();
        raceOngoing = true;
        cts = new CancellationTokenSource();

        Console.Write("Enter the number of ants: ");
        int antCount = int.Parse(Console.ReadLine());

        for (int i = 0; i < antCount; i++)
        {
            Console.Write($"Enter the start point for ant {i + 1}: ");
            int startPoint = int.Parse(Console.ReadLine());

            Console.Write($"Enter the time (in milliseconds) per step for ant {i + 1}: ");
            int timePerStep = int.Parse(Console.ReadLine());

            ants.Add(new Ant(i + 1, startPoint, timePerStep));
        }
    }

    // متد برای شروع مسابقه
    static void StartRace()
    {
        List<Task> tasks = new List<Task>();

        foreach (Ant ant in ants)
        {
            tasks.Add(Task.Run(() => RunRace(ant, cts.Token)));
        }

        Task.Run(() => MonitorRace());
        Task.WhenAll(tasks).Wait();

        DisplayWinner();
    }

    // متد برای اجرای مسابقه هر مورچه
    static void RunRace(Ant ant, CancellationToken token)
    {
        while (raceOngoing && ant.Position < raceLength)
        {
            if (token.IsCancellationRequested)
                return;

            Thread.Sleep(ant.TimePerStep);
            ant.Position++;

            // استفاده از lock برای جلوگیری از تداخل در نمایش وضعیت
            lock (lockObject)
            {
                DisplayStatus();
            }
        }
    }

    // متد برای نمایش وضعیت مسابقه
    static void DisplayStatus()
    {
        Console.Clear();
        using (StreamWriter writer = new StreamWriter(logFilePath, false))
        {
            foreach (Ant ant in ants)
            {
                Console.WriteLine($"Ant {ant.Number}: {ant.Position}");
                writer.WriteLine($"Ant {ant.Number}: {ant.Position}");
            }
        }
    }

    // متد برای نمایش برنده مسابقه
    static void DisplayWinner()
    {
        Ant winner = ants.OrderByDescending(a => a.Position).First();
        Console.WriteLine($"The winner is Ant {winner.Number}!");
    }

    // متد برای نظارت بر مسابقه (توقف، ادامه، شروع مجدد)
    static async Task MonitorRace()
    {
        while (raceOngoing)
        {
            Console.WriteLine("Enter your command (pause/resume/exit): ");
            string command = Console.ReadLine();

            if (command.ToLower() == "pause")
            {
                cts.Cancel();
                raceOngoing = false;
            }
            else if (command.ToLower() == "resume")
            {
                raceOngoing = true;
                cts = new CancellationTokenSource();
                List<Task> tasks = new List<Task>();
                foreach (Ant ant in ants)
                {
                    tasks.Add(Task.Run(() => RunRace(ant, cts.Token)));
                }
                await Task.WhenAll(tasks);
            }
            else if (command.ToLower() == "exit")
            {
                raceOngoing = false;
                cts.Cancel();
                break;
            }

            await Task.Delay(1000);
        }
    }
}

// کلاس برای تعریف مورچه‌ها
class Ant
{
    public int Number { get; }
    public int Position { get; set; }
    public int TimePerStep { get; }

    public Ant(int number, int startPosition, int timePerStep)
    {
        Number = number;
        Position = startPosition;
        TimePerStep = timePerStep;
    }
}
