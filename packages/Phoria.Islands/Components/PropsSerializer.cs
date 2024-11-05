using System.Buffers;
using System.Text.Json;

namespace Phoria.Islands.Components;

internal interface IPropsSerializer
{
	PropsSerialized Serialize(object props);
}

internal class SystemTextJsonPropsSerializer : IPropsSerializer
{
	private readonly JsonSerializerOptions jsonSerializerOptions;

	public SystemTextJsonPropsSerializer(JsonSerializerOptions jsonSerializerOptions)
	{
		this.jsonSerializerOptions = jsonSerializerOptions;
	}

	public PropsSerialized Serialize(object props)
	{
		var pooledStream = new PooledStream();
		JsonSerializer.Serialize(new Utf8JsonWriter((IBufferWriter<byte>)pooledStream.Stream), props, jsonSerializerOptions);

		return new PropsSerialized(pooledStream);
	}
}

