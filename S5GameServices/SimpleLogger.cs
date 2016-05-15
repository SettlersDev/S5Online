using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace S5GameServices
{
    public interface ISimpleLogger
    {
        void WriteDebug(string format, params object[] vals);
        void WriteError(string format, params object[] vals);
    }

    public class NoLogger : ISimpleLogger
    {
        public static ISimpleLogger Instance = new NoLogger();

        public void WriteDebug(string format, params object[] vals) { }

        public void WriteError(string format, params object[] vals) { }
    }

    public class ConsoleLogger : ISimpleLogger
    {
        protected bool logDebug;

        public void WriteDebug(string format, params object[] vals)
        {
            if (logDebug)
                Console.WriteLine(format, vals);
        }

        public void WriteError(string format, params object[] vals)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(format, vals);
            Console.ResetColor();
        }

        public ConsoleLogger(bool logDebug = false)
        {
            this.logDebug = logDebug;
        }
    }

    public class FileLogger : ISimpleLogger
    {
        StreamWriter logStream;
        Timer flushTicker;
        bool logDebug;

        public void WriteDebug(string format, params object[] vals)
        {
            if (logDebug)
                logStream.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + format, vals);
        }

        public void WriteError(string format, params object[] vals)
        {
            logStream.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "ERROR: " + format, vals);
        }

        public FileLogger(string logFilePath, bool logDebug = false)
        {
            this.logDebug = logDebug;
            logStream = new StreamWriter(logFilePath);

            flushTicker = new Timer(5000);
            flushTicker.Elapsed += FlushTicker_Elapsed;
            flushTicker.Enabled = true;
        }

        private void FlushTicker_Elapsed(object sender, ElapsedEventArgs e)
        {
            logStream.Flush();
        }
    }

    public class DualLogger : ISimpleLogger
    {
        ISimpleLogger consoleLogger, fileLogger;
        bool logDebug;

        public void WriteDebug(string format, params object[] vals)
        {
            if (logDebug)
            {
                consoleLogger.WriteDebug(format, vals);
                fileLogger.WriteDebug(format, vals);
            }
        }

        public void WriteError(string format, params object[] vals)
        {
            consoleLogger.WriteError(format, vals);
            fileLogger.WriteError(format, vals);
        }

        public DualLogger(string logFilePath, bool logDebug = false)
        {
            this.logDebug = logDebug;
            consoleLogger = new ConsoleLogger(logDebug);
            fileLogger = new FileLogger(logFilePath, logDebug);
        }
    }
}
