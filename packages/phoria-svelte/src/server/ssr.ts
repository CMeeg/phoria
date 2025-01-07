import { type PhoriaIslandComponentSsrService, createIslandImport } from "@phoria/phoria"
import type { PhoriaIslandSsrRender } from "@phoria/phoria/server"
import type { Component, ComponentProps } from "svelte"
import { render } from "svelte/server"
import { framework } from "~/main"

const renderIslandToString: PhoriaIslandSsrRender<Component> = (island, props) => {
	const ctx = new Map()
	const html = render(island.component, {
		props: props as ComponentProps<typeof island.component>,
		context: ctx
	})

	return html.body
}

interface SvelteSsrOptions {
	renderIsland: PhoriaIslandSsrRender<Component>
}

// TODO: Align naming convention with other frameworks
const ssrOptions: SvelteSsrOptions = {
	// TODO: Svelte doesn't support streaming - should I acknowledge that somehow?
	renderIsland: renderIslandToString
}

// TODO: Export
function configureSvelteSsr(options: Partial<SvelteSsrOptions>) {
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
			componentPath: island.componentPath,
			html
		}
	}
}

export { service }
