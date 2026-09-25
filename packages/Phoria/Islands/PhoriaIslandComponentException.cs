namespace Phoria.Islands;
public class PhoriaIslandComponentException
	: Exception
{
	public PhoriaIslandComponentException()
	{
	}

	public PhoriaIslandComponentException(string message)
		: base(message)
	{
	}

	public PhoriaIslandComponentException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
