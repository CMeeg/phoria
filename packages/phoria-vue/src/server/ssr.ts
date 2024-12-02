import {
	type PhoriaIsland,
	type PhoriaIslandComponentSsrService,
	type PhoriaIslandProps,
	createIslandImport
} from "@phoria/phoria"
import { type Component, createSSRApp } from "vue"
import { renderToWebStream, renderToString as vueRenderToString } from "vue/server-renderer"
import { framework } from "~/main"

async function renderToString<P extends PhoriaIslandProps>(island: PhoriaIsland<Component>, props?: P) {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return vueRenderToString(app, ctx)
}

async function renderToStream<P extends PhoriaIslandProps>(island: PhoriaIsland<Component>, props?: P) {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return renderToWebStream(app, ctx)
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props, options) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		const html =
			(options?.preferStream ?? true) ? await renderToStream(island, props) : await renderToString(island, props)

		return {
			framework: framework.name,
			// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
			componentPath: island.componentPath,
			html
		}
	}
}

export { service }
