using System.Buffers;

namespace Phoria.Islands;

internal class TextWriterBufferWriter : IBufferWriter<char>
{
	private static readonly MemoryPool<char> memoryPool = MemoryPool<char>.Shared;

	private readonly TextWriter textWriter;

	private IMemoryOwner<char>? memoryOwner;

	public TextWriterBufferWriter(TextWriter textWriter)
	{
		this.textWriter = textWriter;
	}

	public void Advance(int count)
	{
		if (memoryOwner == null)
		{
			throw new Exception("Cannot advance. No memory rented.");
		}

		textWriter.Write(memoryOwner.Memory.Span.Slice(0, count));

		memoryOwner.Dispose();
		memoryOwner = null;
	}

	public Memory<char> GetMemory(int sizeHint = 0)
	{
		memoryOwner = memoryPool.Rent(sizeHint);
		return memoryOwner.Memory;
	}

	public Span<char> GetSpan(int sizeHint = 0)
	{
		memoryOwner = memoryPool.Rent(sizeHint);
		return memoryOwner.Memory.Span;
	}
}
