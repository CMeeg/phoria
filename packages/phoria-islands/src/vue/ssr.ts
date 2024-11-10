import type { Component } from "vue"
import type { HttpResponse, PhoriaIsland } from "~/phoria-island-registry"

async function sendHttpResponse<P extends Record<string, unknown> | null>(
	res: HttpResponse,
	island: PhoriaIsland<Component>,
	props?: P
) {
	await Promise.all([import("vue"), import("vue/server-renderer")])
		.then(([Vue, VueServer]) => {
			const app = Vue.createSSRApp(island.component, props)
			const ctx = {}
			return VueServer.renderToString(app, ctx)
		})
		.then((html) => {
			res.status(200).setHeader("Content-Type", "text/html").send(html)
		})
}

async function streamHttpResponse<P extends Record<string, unknown> | null>(
	res: HttpResponse,
	island: PhoriaIsland<Component>,
	props?: P
) {
	const stream = await Promise.all([import("vue"), import("vue/server-renderer")]).then(([Vue, VueServer]) => {
		const app = Vue.createSSRApp(island.component, props)
		const ctx = {}
		const stream = VueServer.renderToWebStream(app, ctx) as unknown as NodeJS.ReadableStream

		return stream
	})

	for await (const chunk of stream) {
		if (res.closed) {
			break
		}

		res.write(chunk)
	}

	res.end()
}

export { sendHttpResponse, streamHttpResponse }
