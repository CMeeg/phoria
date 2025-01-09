import { type PhoriaIslandProps, createIslandImport } from "@phoria/phoria"
import type { PhoriaIslandComponentSsrService, RenderPhoriaIslandComponent } from "@phoria/phoria/server"
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

const service: PhoriaIslandComponentSsrService<Component> = {
	render: async (component, props, options) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		const renderComponent = options?.renderComponent ?? renderComponentToStream

		const html = await renderComponent(island, props)

		return {
			framework: framework.name,
			// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, renderComponentToStream, renderComponentToString }

export type { RenderVuePhoriaIslandComponent }
