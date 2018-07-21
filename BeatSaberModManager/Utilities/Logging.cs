using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BeatSaberModManager.Utilities.Logging
{
    public abstract class LoggerBase
    {
        public enum Level : byte
        {
            None = 0,
            Debug = 1,
            Info = 2,
            Warning = 4,
            Error = 8,
            Critical = 16,
            SuperVerbose = 32
        }

        public abstract void Log(Level level, string message);
        public void Log(Level level, Exception exeption) => Log(level, exeption.ToString());
        public void SuperVerbose(string message) => Log(Level.SuperVerbose, message);
        public void SuperVerbose(Exception e) => Log(Level.SuperVerbose, e);
        public void Debug(string message) => Log(Level.Debug, message);
        public void Debug(Exception e) => Log(Level.Debug, e);
        public void Info(string message) => Log(Level.Info, message);
        public void Info(Exception e) => Log(Level.Info, e);
        public void Warn(string message) => Log(Level.Warning, message);
        public void Warn(Exception e) => Log(Level.Warning, e);
        public void Error(string message) => Log(Level.Error, message);
        public void Error(Exception e) => Log(Level.Error, e);
        public void Critical(string message) => Log(Level.Critical, message);
        public void Critical(Exception e) => Log(Level.Critical, e);
    }

    public class Logger : LoggerBase
    {
        private static Logger _log = null;// = CreateLogger(Assembly.GetCallingAssembly().GetName().Name);
        internal static Logger log
        {
            get
            {
                if (_log == null)
                    _log = CreateLogger(ManagerPlugin.GetName());
                return _log;
            }
        }

        internal static Logger CreateLogger(string loggerName)
        {
            return new Logger(loggerName);
        }
        
        [Flags]
        public enum LogLevel : byte
        {
            None = Level.None,
            SuperVerboseOnly = Level.SuperVerbose,
            DebugOnly = Level.Debug,
            InfoOnly = Level.Info,
            WarningOnly = Level.Warning,
            ErrorOnly = Level.Error,
            CriticalOnly = Level.Critical,

            ErrorUp = ErrorOnly | CriticalOnly,
            WarningUp = WarningOnly | ErrorUp,
            InfoUp = InfoOnly | WarningUp,
            All = DebugOnly | InfoUp,

            ReallyNotReccomendedAll = SuperVerboseOnly | All,
        }

        private static List<ILogPrinter> defaultPrinters = new List<ILogPrinter>()
        {
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.SuperVerboseOnly,
                Color = ConsoleColor.Cyan,
            },
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.DebugOnly,
                Color = ConsoleColor.Green,
            },
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.InfoOnly,
                Color = ConsoleColor.White,
            },
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.WarningOnly,
                Color = ConsoleColor.Yellow,
            },
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.ErrorOnly,
                Color = ConsoleColor.Red,
            },
            new ColoredConsolePrinter()
            {
                Filter = LogLevel.CriticalOnly,
                Color = ConsoleColor.Magenta,
            },
            new FilePrinter("modmanager.log")
        };

        private string logName;
        private static LogLevel showFilter = LogLevel.InfoUp;
        public static LogLevel Filter { get => showFilter; set => showFilter = value; }
        private List<ILogPrinter> printers = defaultPrinters;

        private Logger(string name)
        {
            logName = name;
        }

        public override void Log(Level level, string message)
        {
            foreach (var printer in printers)
                if (((byte)level & (byte)printer.Filter) != 0)
                    printer.Print(level, logName, message);
        }
    }

    public interface ILogPrinter
    {
        Logger.LogLevel Filter { get; set; }
        void Print(LoggerBase.Level level, string logName, string message);
    }

    public class ColoredConsolePrinter : ILogPrinter
    {
        Logger.LogLevel filter = Logger.LogLevel.All;
        public Logger.LogLevel Filter { get => filter; set => filter = value; }

        ConsoleColor color = Console.ForegroundColor;
        public ConsoleColor Color { get => color; set => color = value; }

        public void Print(LoggerBase.Level level, string logName, string message)
        {
            if (((byte)level & (byte)Logger.Filter) == 0) return;
            Console.ForegroundColor = color;
            foreach (var line in message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                Console.WriteLine($"[{logName}][{level.ToString()}] {line}");
            //Console.ForegroundColor = ConsoleColor.Gray; // reset to "default"
        }
    }

    public class FilePrinter : ILogPrinter
    {
        private FileInfo file;
        private StreamWriter output;

        public FilePrinter(string filename)
        {
            Filter = Logger.LogLevel.All;
            file = new FileInfo(filename);
            if (!file.Exists)
                output = file.CreateText();
            else
                output = file.AppendText();

            string ds = DateTime.Now.ToString();
            output.WriteLine($"--------------------------------{new string('-', ds.Length)}----");
            output.WriteLine($"    BEAT SABER SESSION STARTING {ds}");
            output.WriteLine($"--------------------------------{new string('-', ds.Length)}----");
        }

        public Logger.LogLevel Filter { get; set; }

        public void Print(LoggerBase.Level level, string logName, string message)
        {
            var timestring = DateTime.Now.ToString();
            foreach (var line in message.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                output.WriteLine($"[{timestring}][{logName}][{level.ToString()}] {line}");
            output.Flush();
        }
    }
}
