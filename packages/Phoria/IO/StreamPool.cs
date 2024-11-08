// Copyright (c) 2024 Daniil Sokolyuk.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.IO;

namespace Phoria.IO;

public sealed class StreamPool
	: IDisposable
{
	private static readonly RecyclableMemoryStreamManager manager;

	public RecyclableMemoryStream Stream { get; }

	static StreamPool()
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

	public StreamPool()
	{
		Stream = manager.GetStream();
	}

	public void Dispose()
	{
		Stream?.Dispose();
	}
}
