using System.Net;
using Phoria.IO;
using Phoria.Islands;

namespace Phoria.Tests.TestUtilities;

internal sealed class TrackingSsr : IPhoriaIslandSsr
{
	public int CallCount { get; private set; }

	public Task<PhoriaIslandSsrResult> RenderIsland(
		PhoriaIsland island,
		CancellationToken cancellationToken = default)
	{
		CallCount++;
		return Task.FromResult(new PhoriaIslandSsrResult
		{
			Headers = new HttpResponseMessage().Headers,
			Content = new StreamPool()
		});
	}
}
