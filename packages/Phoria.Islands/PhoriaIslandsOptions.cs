using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Phoria.Islands.Components;

namespace Phoria.Islands;

public class PhoriaIslandsOptions
{
	internal IPropsSerializer PropsSerializer { get; set; }

	public PhoriaIslandsOptions()
	{
		PropsSerializer = ConfigureSystemTextJsonPropsSerializer(_ => { });
	}

	/// <summary>
	/// Handle an exception caught during server-render of a component.
	/// If unset, unhandled exceptions will be thrown for all component renders.
	/// </summary>
	public Action<Exception, string, string> ExceptionHandler { get; set; } = (ex, ComponentName, ContainerId) =>
		throw new Exception(string.Format(
			"Error while rendering \"{0}\" to \"{2}\": {1}",
			ComponentName,
			ex.Message,
			ContainerId
		));

	/// <summary>
	/// Set System.Text.Json serializer for island component props.
	/// </summary>
	/// <param name="configureJsonSerializerOptions"></param>
	private IPropsSerializer ConfigureSystemTextJsonPropsSerializer(Action<JsonSerializerOptions> configureJsonSerializerOptions)
	{
		var jsonSerializerOptions = new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNameCaseInsensitive = true,
		};

		configureJsonSerializerOptions(jsonSerializerOptions);
		return new SystemTextJsonPropsSerializer(jsonSerializerOptions);
	}
}
