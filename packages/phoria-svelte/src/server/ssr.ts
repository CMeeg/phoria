import { type PhoriaIslandComponentSsrService, type PhoriaIslandProps, createIslandImport } from "@phoria/phoria"
import type { RenderPhoriaIslandComponent } from "@phoria/phoria"
import type { Component, ComponentProps } from "svelte"
import { render } from "svelte/server"
import { framework } from "~/main"

type RenderSveltePhoriaIslandComponent<P = PhoriaIslandProps> = RenderPhoriaIslandComponent<Component, P>

const renderComponentToString: RenderSveltePhoriaIslandComponent = (island, props) => {
	const ctx = new Map()
	const html = render(island.component, {
		props: props as ComponentProps<typeof island.component>,
		context: ctx
	})

	return html.body
}

const service: PhoriaIslandComponentSsrService<Component> = {
	render: async (component, props, options) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		const renderComponent = options?.renderComponent ?? renderComponentToString

		const html = await renderComponent(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, renderComponentToString }

export type { RenderSveltePhoriaIslandComponent }
