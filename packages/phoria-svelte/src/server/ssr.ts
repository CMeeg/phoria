import { type PhoriaIslandComponentSsrService, type PhoriaIslandProps, createIslandImport } from "@phoria/phoria"
import type { RenderPhoriaIslandComponent } from "@phoria/phoria/server"
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

interface PhoriaSvelteSsrOptions {
	renderComponent: RenderSveltePhoriaIslandComponent
}

const ssrOptions: PhoriaSvelteSsrOptions = {
	// TODO: Svelte doesn't support streaming - should I acknowledge that somehow?
	renderComponent: renderComponentToString
}

function configureSvelteSsrService(options: Partial<PhoriaSvelteSsrOptions>) {
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
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, configureSvelteSsrService, renderComponentToString }

export type { PhoriaSvelteSsrOptions, RenderSveltePhoriaIslandComponent }
