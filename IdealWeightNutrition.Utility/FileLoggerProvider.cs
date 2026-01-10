using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Simple file logger provider that writes logs to daily files
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();

        public FileLoggerProvider(string logDirectory)
        {
            _logDirectory = logDirectory;
            
            // Ensure log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    /// <summary>
    /// File logger that writes to daily log files
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logDirectory;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, string logDirectory)
        {
            _categoryName = categoryName;
            _logDirectory = logDirectory;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var logFileName = Path.Combine(_logDirectory, $"app-{DateTimeHelper.Now:yyyy-MM-dd}.log");
            var logMessage = $"[{DateTimeHelper.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {formatter(state, exception)}";
            
            if (exception != null)
            {
                logMessage += Environment.NewLine + exception.ToString();
            }

            logMessage += Environment.NewLine;

            // Thread-safe file writing
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(logFileName, logMessage);
                }
                catch
                {
                    // Silently fail if unable to write to log file
                }
            }
        }
    }

    /// <summary>
    /// Extension methods for file logger
    /// </summary>
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string logDirectory)
        {
            builder.AddProvider(new FileLoggerProvider(logDirectory));
            return builder;
        }
    }
}

