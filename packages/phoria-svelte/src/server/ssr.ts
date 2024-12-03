import {
	type PhoriaIsland,
	type PhoriaIslandComponentSsrService,
	type PhoriaIslandProps,
	createIslandImport
} from "@phoria/phoria"
import type { Component, ComponentProps } from "svelte"
import { render } from "svelte/server"
import { framework } from "~/main"

async function renderToString<P extends PhoriaIslandProps>(island: PhoriaIsland<Component>, props?: P) {
	const ctx = new Map()
	const html = render(island.component, {
		props: props as ComponentProps<typeof island.component>,
		context: ctx
	})

	return html.body
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<Component>(component)
		const island = await islandImport

		// TODO: Svelte doesn't support streaming - should I acknowledge that somehow?
		const html = await renderToString(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service }
