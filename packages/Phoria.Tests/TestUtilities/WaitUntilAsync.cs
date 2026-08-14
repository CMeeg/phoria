namespace Phoria.Tests.TestUtilities;

internal static class AsyncTestWaits
{
	public static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow.Add(timeout);
		while (!condition())
		{
			if (DateTime.UtcNow >= deadline)
			{
				throw new TimeoutException($"Condition was not met within {timeout}.");
			}

			await Task.Delay(100);
		}
	}
}
