// Copyright (c) 2024 Daniil Sokolyuk.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.IO;
using Phoria.IO;

namespace Phoria.Islands;

public sealed class SerializedProps
	: IDisposable
{
	private readonly PooledStream pooledStream;

	public RecyclableMemoryStream Stream => pooledStream.Stream;

	public SerializedProps(PooledStream pooledStream)
	{
		this.pooledStream = pooledStream;
	}

	public void Dispose()
	{
		pooledStream?.Dispose();
	}
}
