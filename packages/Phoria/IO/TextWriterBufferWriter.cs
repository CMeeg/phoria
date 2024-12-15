// Copyright (c) 2024 Daniil Sokolyuk.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Buffers;
using System.Text.Encodings.Web;

namespace Phoria.IO;

internal sealed class TextWriterBufferWriter
	: IBufferWriter<char>
{
	private static readonly MemoryPool<char> memoryPool = MemoryPool<char>.Shared;

	private readonly TextWriter textWriter;
	private readonly TextEncoder? encoder;

	private IMemoryOwner<char>? memoryOwner;

	public TextWriterBufferWriter(TextWriter textWriter) => this.textWriter = textWriter;

	public TextWriterBufferWriter(TextWriter textWriter, TextEncoder? encoder)
	{
		this.textWriter = textWriter;
		this.encoder = encoder;
	}

	public void Advance(int count)
	{
		if (memoryOwner == null)
		{
			throw new InvalidOperationException("Cannot advance. No memory rented.");
		}

		if (encoder != null)
		{
			char[] array = new char[count * 4];
			Span<char> output = array;
			encoder.Encode(
				memoryOwner.Memory.Span.Slice(0, count),
				output,
				out _,
				out int charsWritten);

			textWriter.Write(output.Slice(0, charsWritten));
		}
		else
		{
			textWriter.Write(memoryOwner.Memory.Span.Slice(0, count));
		}

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
