// Copyright (c) 2024 Daniil Sokolyuk.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.IO;

namespace Phoria.IO;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "The name reflects the purpose of the class.")]
public sealed class PooledStream
	: IDisposable
{
	private static readonly RecyclableMemoryStreamManager manager;

	public RecyclableMemoryStream Stream { get; }

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
		Stream = manager.GetStream();
	}

	public void Dispose()
	{
		Stream?.Dispose();
	}
}
