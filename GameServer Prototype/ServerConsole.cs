using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameServer_Prototype
{
    public static class ServerConsole
    {

        static string NowTime
        {
            get
            {
                return DateTime.Now.ToString("HH:mm:ss");
            }
        }

        public static void Log(string message) {
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("["+NowTime+"] " + message);
            Console.ResetColor();
        }
        public static void LogWarning(string message) {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("[" + NowTime + "] " + message);
            Console.ResetColor();
        }
        public static void LogError(string message) {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("[" + NowTime + "] " + message);
            Console.ResetColor();
        }
    }
}
