using System.Text.Json;
using Phoria.Islands;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandPropsSerializerTests
{
	[Fact]
	public void Serialize_WithCamelCaseOptions_ProducesCamelCaseJson()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		var serializer = new SystemTextJsonPropsSerializer(options);

		var result = serializer.Serialize(new { FirstName = "Ada" });

		Assert.Equal("{\"firstName\":\"Ada\"}", result);
	}

	[Fact]
	public void Serialize_WithConfigureAction_AppliesConfiguredOptions()
	{
		var serializer = new SystemTextJsonPropsSerializer(o => o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

		var result = serializer.Serialize(new { LastName = "Lovelace" });

		Assert.Equal("{\"lastName\":\"Lovelace\"}", result);
	}
}
