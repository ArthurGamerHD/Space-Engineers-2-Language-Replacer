using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace SE2_Language_Replacer.Lib;

public class FileLoggerProvider(StreamWriter logFileWriter) : ILoggerProvider
{
    private readonly StreamWriter _logFileWriter = logFileWriter ?? throw new ArgumentNullException(nameof(logFileWriter));

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logFileWriter);
    }

    public void Dispose()
    {
        _logFileWriter.Dispose();
    }
}

// Customized ILogger, writes logs to text files
public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly StreamWriter _logFileWriter;

    public FileLogger(string categoryName, StreamWriter logFileWriter)
    {
        _categoryName = categoryName;
        _logFileWriter = logFileWriter;
    }

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public  IDisposable? BeginScope<TState>(TState state)
    {
        return null;
    }
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

    public bool IsEnabled(LogLevel logLevel)
    {
        // Ensure that only information level and higher logs are recorded
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Ensure that only information level and higher logs are recorded
        if (!IsEnabled(logLevel))
        {
            return;
        }

        // Get the formatted log message
        var message = formatter(state, exception);

        //Write log messages to text file
        _logFileWriter.WriteLine($"[{logLevel}] [{DateTime.Now:HH:mm:ss}] [{_categoryName}] {message}");
        _logFileWriter.Flush();
    }
}