using System.Buffers;
using System.Text.Json;
using Phoria.IO;

namespace Phoria.Islands;

public interface IPhoriaIslandPropsSerializer
{
	string Serialize(object props);
	void Serialize(object props, StreamPool streamPool);
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

	public string Serialize(object props)
	{
		return JsonSerializer.Serialize(
			props,
			jsonSerializerOptions);
	}

	public void Serialize(object props, StreamPool streamPool)
	{
		JsonSerializer.Serialize(
			new Utf8JsonWriter((IBufferWriter<byte>)streamPool.Stream),
			props,
			jsonSerializerOptions);
	}
}
