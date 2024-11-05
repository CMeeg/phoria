using Microsoft.IO;

namespace Phoria.Islands.Components;

internal class PropsSerialized : IDisposable
{
    private readonly PooledStream pooledStream;

    public RecyclableMemoryStream Stream => pooledStream.Stream;

    public PropsSerialized(PooledStream pooledStream)
    {
        this.pooledStream = pooledStream;
    }

    public void Dispose()
    {
        pooledStream?.Dispose();
    }
}
