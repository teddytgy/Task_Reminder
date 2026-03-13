using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task_Reminder.Wpf.Models;

namespace Task_Reminder.Wpf.Logging;

public sealed class FileLoggerProvider(IOptions<FileLoggingOptions> options) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileLoggingOptions _options = options.Value;

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, _ => new FileLogger(categoryName, _options));

    public void Dispose()
    {
    }

    private sealed class FileLogger(string categoryName, FileLoggingOptions options) : ILogger
    {
        private static readonly object SyncRoot = new();
        private readonly string _categoryName = categoryName;
        private readonly FileLoggingOptions _options = options;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            if (!_options.Enabled || logLevel == LogLevel.None)
            {
                return false;
            }

            return logLevel >= GetMinimumLevel(_options.MinimumLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var logFilePath = ResolveLogFilePath(Environment.ExpandEnvironmentVariables(_options.Path));
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var message = formatter(state, exception);
            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {_categoryName} {message}";
            if (exception is not null)
            {
                line = $"{line}{Environment.NewLine}{exception}";
            }

            lock (SyncRoot)
            {
                ApplyRetention(logFilePath);
                File.AppendAllText(logFilePath, line + Environment.NewLine, System.Text.Encoding.UTF8);
            }
        }

        private static LogLevel GetMinimumLevel(string? configuredLevel) =>
            Enum.TryParse(configuredLevel, ignoreCase: true, out LogLevel parsedLevel)
                ? parsedLevel
                : LogLevel.Information;

        private string ResolveLogFilePath(string configuredPath)
        {
            if (!_options.RollDaily)
            {
                return configuredPath;
            }

            var directory = Path.GetDirectoryName(configuredPath) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(configuredPath);
            var extension = Path.GetExtension(configuredPath);
            return Path.Combine(directory, $"{fileNameWithoutExtension}-{DateTime.UtcNow:yyyyMMdd}{extension}");
        }

        private void ApplyRetention(string currentLogFilePath)
        {
            var limit = Math.Max(1, _options.RetainedFileCountLimit);
            var directory = Path.GetDirectoryName(currentLogFilePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            var prefix = $"{Path.GetFileNameWithoutExtension(_options.Path)}-";
            var extension = Path.GetExtension(_options.Path);
            var oldFiles = Directory.GetFiles(directory, $"{prefix}*{extension}")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Skip(limit)
                .ToList();

            foreach (var oldFile in oldFiles)
            {
                File.Delete(oldFile);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
