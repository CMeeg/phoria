import type { Component, ComponentProps } from "svelte"
import type { HttpResponse, PhoriaIsland } from "~/phoria-island-registry"

async function sendHttpResponse<P extends Record<string, unknown> | null>(
	res: HttpResponse,
	island: PhoriaIsland<Component>,
	props?: P
) {
	await Promise.all([import("svelte/server")])
		.then(([SvelteServer]) => {
			const ctx = new Map()
			const html = SvelteServer.render(island.component, {
				props: props as ComponentProps<typeof island.component>,
				context: ctx
			})

			return html.body
		})
		.then((html) => {
			res.status(200).setHeader("Content-Type", "text/html").send(html)
		})
}

export { sendHttpResponse }
