using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Logging {
    class Logger {

        public static void Trace(string message) {
            Log(message, LogLevel.Trace, null);
        }

        public static void Info(string message) {
            Log(message, LogLevel.Info, null);
        }

        public static void Alert(string message) {
            Log(message, LogLevel.Alert, null);
        }

        public static void Debug(string message) {
            Log(message, LogLevel.Debug, null);
        }

        public static void Warn(string message) {
            Log(message, LogLevel.Warn, null);
        }

        public static void Error(string message) {
            Log(message, LogLevel.Error, null);
        }

        public static void Error(string message, Exception e) {
            Log(message, LogLevel.Error, e);
        }

        public static void Log(string message, LogLevel level, Exception e) {
            if (e != null) {
                message = message + Environment.NewLine + e.Message + Environment.NewLine + e.StackTrace;
            }
            Mod.i.Monitor.Log(message, level);
        }
    }
}
