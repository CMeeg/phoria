using System.Diagnostics;

namespace Phoria.Diagnostics;

public static class PhoriaActivitySource
{
	public const string Name = "Phoria";
	public static readonly ActivitySource Instance = new(Name, typeof(PhoriaActivitySource).Assembly.GetName().Version?.ToString());
}
