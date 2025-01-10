import { type PhoriaIslandProps, importComponent } from "@phoria/phoria"
import type { PhoriaIsland, PhoriaIslandComponentSsrService, RenderPhoriaIslandComponent } from "@phoria/phoria/server"
import { type Component, createSSRApp } from "vue"
import { renderToString, renderToWebStream } from "vue/server-renderer"
import { framework } from "~/main"

type RenderVuePhoriaIslandComponent<P extends PhoriaIslandProps = PhoriaIslandProps> = RenderPhoriaIslandComponent<
	typeof framework.name,
	Component,
	P
>

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

type VuePhoriaIsland = PhoriaIsland<typeof framework.name, Component>

function isVueIsland(island: PhoriaIsland): island is VuePhoriaIsland {
	return island.framework === framework.name
}

const service: PhoriaIslandComponentSsrService<typeof framework.name, Component> = {
	render: async (component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		// TODO: Can "cache" the imported component? Maybe only in production?
		const island = await importComponent<typeof framework.name, Component>(component)

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

export { isVueIsland, renderComponentToStream, renderComponentToString, service }

export type { RenderVuePhoriaIslandComponent, VuePhoriaIsland }
