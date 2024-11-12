import type { Component } from "vue"
import type { PhoriaIsland, PhoriaIslandProps } from "~/phoria-island-registry"

// TODO: With some refactoring we shouldn't have to dynamically import the required modules for SSR

async function renderToString<P extends PhoriaIslandProps>(island: PhoriaIsland<Component>, props?: P) {
	return await Promise.all([import("vue"), import("vue/server-renderer")])
		.then(([Vue, VueServer]) => {
			const app = Vue.createSSRApp(island.component, props)
			const ctx = {}
			return VueServer.renderToString(app, ctx)
		})
		.then((html) => {
			return html
		})
}

async function renderToStream<P extends PhoriaIslandProps>(island: PhoriaIsland<Component>, props?: P) {
	return await Promise.all([import("vue"), import("vue/server-renderer")]).then(([Vue, VueServer]) => {
		const app = Vue.createSSRApp(island.component, props)
		const ctx = {}
		return VueServer.renderToWebStream(app, ctx)
	})
}

export { renderToString, renderToStream }
