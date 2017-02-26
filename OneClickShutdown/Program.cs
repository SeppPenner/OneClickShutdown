using System;
using System.Diagnostics;
using System.Reflection;

namespace OneClickShutdown
{
    public static class Program
    {
        public static void Main()
        {
            Console.Title = Assembly.GetExecutingAssembly().GetName().Name + " " +
                            Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine("Windows wird heruntergefahren...");
            Console.WriteLine("Beliebige Taste zum Beenden drücken...");
            Process.Start("shutdown", "/s /t 0");
            Console.ReadKey();
        }
    }
}