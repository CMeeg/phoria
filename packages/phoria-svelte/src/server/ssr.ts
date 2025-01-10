import { type PhoriaIslandProps, importComponent } from "@phoria/phoria"
import type { PhoriaIsland, PhoriaIslandComponentSsrService, RenderPhoriaIslandComponent } from "@phoria/phoria/server"
import type { Component, ComponentProps } from "svelte"
import { render } from "svelte/server"
import { framework } from "~/main"

type RenderSveltePhoriaIslandComponent<P extends PhoriaIslandProps = PhoriaIslandProps> = RenderPhoriaIslandComponent<
	typeof framework.name,
	Component,
	P
>

const renderComponentToString: RenderSveltePhoriaIslandComponent = (island, props) => {
	const ctx = new Map()
	const html = render(island.component, {
		props: props as ComponentProps<typeof island.component>,
		context: ctx
	})

	return html.body
}

type SveltePhoriaIsland = PhoriaIsland<typeof framework.name, Component>

function isSvelteIsland(island: PhoriaIsland): island is SveltePhoriaIsland {
	return island.framework === framework.name
}

const service: PhoriaIslandComponentSsrService<typeof framework.name, Component> = {
	render: async (component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		// TODO: Can "cache" the imported component? Maybe only in production?
		const island = await importComponent<typeof framework.name, Component>(component)

		const renderComponent = options?.renderComponent ?? renderComponentToString

		const html = await renderComponent(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { isSvelteIsland, renderComponentToString, service }

export type { RenderSveltePhoriaIslandComponent, SveltePhoriaIsland }
