import { type PhoriaIslandComponentSsrService, type PhoriaIslandProps, createIslandImport } from "@phoria/phoria"
import type { RenderPhoriaIslandComponent } from "@phoria/phoria/server"
import { type Component, createSSRApp } from "vue"
import { renderToWebStream, renderToString } from "vue/server-renderer"
import { framework } from "~/main"

type RenderVuePhoriaIslandComponent<P = PhoriaIslandProps> = RenderPhoriaIslandComponent<Component, P>

const renderComponentToString: RenderVuePhoriaIslandComponent = (island, props) => {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return renderToString(app, ctx)
}

const renderComponentToStream: RenderVuePhoriaIslandComponent = async (island, props) => {
	const app = createSSRApp(island.component, props)
	const ctx = {}
	return renderToWebStream(app, ctx)
}

interface PhoriaVueSsrOptions {
	renderComponent: RenderVuePhoriaIslandComponent
}

const ssrOptions: PhoriaVueSsrOptions = {
	renderComponent: renderComponentToStream
}


function configureVueSsrService(options: Partial<PhoriaVueSsrOptions>) {
	if (typeof options.renderComponent !== "undefined") {
		ssrOptions.renderComponent = options.renderComponent
	}
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		const html = await ssrOptions.renderComponent(island, props)

		return {
			framework: framework.name,
			// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, configureVueSsrService, renderComponentToStream, renderComponentToString }

export type { PhoriaVueSsrOptions, RenderVuePhoriaIslandComponent }
