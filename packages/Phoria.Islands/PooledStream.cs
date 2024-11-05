using Microsoft.IO;

namespace Phoria.Islands;

internal class PooledStream : IDisposable
{
	private static readonly RecyclableMemoryStreamManager manager;

	private readonly RecyclableMemoryStream stream;

	public RecyclableMemoryStream Stream => stream;

	static PooledStream()
	{
		int blockSize = 1024 * 128;
		int largeBufferMultiple = 1024 * 1024;
		int maximumBufferSize = 128 * 1024 * 1024;

		manager = new(new RecyclableMemoryStreamManager.Options
		{
			BlockSize = blockSize,
			LargeBufferMultiple = largeBufferMultiple,
			MaximumBufferSize = maximumBufferSize,
			GenerateCallStacks = true,
			AggressiveBufferReturn = true,
			MaximumLargePoolFreeBytes = largeBufferMultiple * 4,
			MaximumSmallPoolFreeBytes = 250 * blockSize
		});
	}

	public PooledStream()
	{
		stream = manager.GetStream();
	}

	public void Dispose()
	{
		stream?.Dispose();
	}
}
