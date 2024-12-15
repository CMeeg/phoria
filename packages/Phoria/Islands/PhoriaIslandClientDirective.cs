using System.Globalization;

namespace Phoria.Islands;

public abstract class PhoriaIslandClientDirective
{
	public abstract string Name { get; }
	public string? Value { get; protected set; }
}

public class PhoriaIslandClientOnlyDirective
	: PhoriaIslandClientDirective
{
	public override string Name => "client:only";
}

public class PhoriaIslandClientLoadDirective
	: PhoriaIslandClientDirective
{
	public override string Name => "client:load";
}

public class PhoriaIslandClientIdleDirective
	: PhoriaIslandClientDirective
{
	public override string Name => "client:idle";

	public PhoriaIslandClientIdleDirective(int? timeout = null)
	{
		if (timeout is not null and > 0)
		{
			Value = timeout.Value.ToString(CultureInfo.InvariantCulture);
		}
	}
}

public class PhoriaIslandClientVisibleDirective
	: PhoriaIslandClientDirective
{
	public override string Name => "client:visible";

	public PhoriaIslandClientVisibleDirective(string? rootMargin = null) => Value = rootMargin;
}

public class PhoriaIslandClientMediaDirective
	: PhoriaIslandClientDirective
{
	public override string Name => "client:media";

	public PhoriaIslandClientMediaDirective(string mediaQuery) => Value = mediaQuery;
}
