using System;

namespace StarbotLib.Logging {
    public class CLogger
    {
        public static object locker = new object();

        public static void Trace(string message)
        {
            Log(message, ConsoleColor.Gray, null);
        }

        public static void Info(string message)
        {
            Log(message, ConsoleColor.White, null);
        }

        public static void Alert(string message)
        {
            Log(message, ConsoleColor.Green, null);
        }

        public static void Warn(string message)
        {
            Log(message, ConsoleColor.Yellow, null);
        }

        public static void Error(string message)
        {
            Log(message, ConsoleColor.Red, null);
        }

        public static void Error(string message, Exception e)
        {
            Log(message, ConsoleColor.Red, e);
        }

        public static void Log(string message, ConsoleColor color, Exception e)
        {
            lock (locker)
            {
                Console.ForegroundColor = color;
                if (e != null)
                {
                    message = message + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace;
                }
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
    }
}
