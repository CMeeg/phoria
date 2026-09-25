using Microsoft.Extensions.Logging;

namespace Phoria.Tests.TestUtilities;

internal sealed class ListLogger<T>(IList<string>? messages = null) : ILogger<T>
{
	private readonly IList<string> messages = messages ?? new List<string>();
	private readonly List<LogEntry> entries = [];

	public IList<string> Messages => messages;

	public IReadOnlyList<LogEntry> Entries => entries;

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => null;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		entries.Add(new LogEntry(logLevel, eventId.Id, formatter(state, exception)));
		messages.Add(formatter(state, exception));
	}

	public sealed record LogEntry(LogLevel Level, int EventId, string Message);
}
