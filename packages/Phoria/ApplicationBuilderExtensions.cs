using Microsoft.AspNetCore.Builder;
using Phoria.Server;

namespace Phoria;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UsePhoria(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);

		return app.UseMiddleware<PhoriaServerMiddleware>();
	}
}
