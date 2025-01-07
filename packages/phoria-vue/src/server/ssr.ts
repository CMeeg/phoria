import { type PhoriaIslandComponentSsrService, createIslandImport } from "@phoria/phoria"
import type { PhoriaIslandSsrRender } from "@phoria/phoria/server"
import { type Component, createSSRApp } from "vue"
import { renderToWebStream, renderToString as vueRenderToString } from "vue/server-renderer"
import { framework } from "~/main"

const renderIslandToString: PhoriaIslandSsrRender<Component> = (island, props) => {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return vueRenderToString(app, ctx)
}

const renderIslandToStream: PhoriaIslandSsrRender<Component> = async (island, props) => {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return renderToWebStream(app, ctx)
}

// TODO: Align naming convention with other frameworks
interface VueSsrOptions {
	renderIsland: PhoriaIslandSsrRender<Component>
}

const ssrOptions: VueSsrOptions = {
	renderIsland: renderIslandToStream
}


// TODO: Export
function configureVueSsr(options: Partial<VueSsrOptions>) {
	if (typeof options.renderIsland !== "undefined") {
		ssrOptions.renderIsland = options.renderIsland
	}
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		const html = await ssrOptions.renderIsland(island, props)

		return {
			framework: framework.name,
			// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
			componentPath: island.componentPath,
			html
		}
	}
}

export { service }
