using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace UserManager.Infrastructure.IntegrationTests.TestDoubles;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<LogLevel> _levels = new();

    public bool HasErrors => _levels.Any(level => level >= LogLevel.Error);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        _levels.Enqueue(logLevel);
    }
}
