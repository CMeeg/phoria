// Copyright (c) 2024 Daniil Sokolyuk.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Buffers;
using System.Text.Json;
using Phoria.IO;

namespace Phoria.Islands;

public interface IPhoriaIslandPropsSerializer
{
	SerializedProps Serialize(object props);
}

public sealed class SystemTextJsonPropsSerializer
	: IPhoriaIslandPropsSerializer
{
	private readonly JsonSerializerOptions jsonSerializerOptions;

	public SystemTextJsonPropsSerializer(JsonSerializerOptions jsonSerializerOptions)
	{
		this.jsonSerializerOptions = jsonSerializerOptions;
	}

	public SystemTextJsonPropsSerializer(Action<JsonSerializerOptions> configure)
	{
		var jsonSerializerOptions = new JsonSerializerOptions();
		configure(jsonSerializerOptions);
		this.jsonSerializerOptions = jsonSerializerOptions;
	}

	public SerializedProps Serialize(object props)
	{
		var pooledStream = new PooledStream();

		JsonSerializer.Serialize(
			new Utf8JsonWriter((IBufferWriter<byte>)pooledStream.Stream),
			props,
			jsonSerializerOptions);

		return new SerializedProps(pooledStream);
	}
}
