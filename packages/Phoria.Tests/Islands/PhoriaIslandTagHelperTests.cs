using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandTagHelperTests
{
	[Fact]
	public async Task Process_ComponentExceptionInDegradeMode_SuppressesOutput()
	{
		var tagHelper = new PhoriaIslandTagHelper(
			new ThrowingComponentFactory(),
			Options.Create(new PhoriaOptions()))
		{
			Component = "my-component"
		};

		TagHelperOutput output = CreateTagHelperOutput();

		await tagHelper.ProcessAsync(CreateTagHelperContext(), output);

		Assert.Null(output.TagName);
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public async Task Process_ComponentExceptionInFailMode_Rethrows()
	{
		var tagHelper = new PhoriaIslandTagHelper(
			new ThrowingComponentFactory(),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}))
		{
			Component = "my-component"
		};

		await Assert.ThrowsAsync<PhoriaIslandComponentException>(
			() => tagHelper.ProcessAsync(CreateTagHelperContext(), CreateTagHelperOutput()));
	}

	private static TagHelperContext CreateTagHelperContext() =>
		new(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

	private static TagHelperOutput CreateTagHelperOutput() =>
		new(
			"phoria-island",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

	private sealed class ThrowingComponentFactory : IPhoriaIslandComponentFactory
	{
		public Task<PhoriaIslandHtmlContent> CreateAsync(
			string component,
			object? props,
			PhoriaIslandClientDirective? client) =>
			throw new PhoriaIslandComponentException("Server is not healthy.");
	}
}
