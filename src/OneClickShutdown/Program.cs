// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   The main program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OneClickShutdown
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// The main program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main method.
        /// </summary>
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