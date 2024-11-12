import type { Component, ComponentProps } from "svelte"
import type { PhoriaIsland, PhoriaIslandProps } from "~/phoria-island-registry"

// TODO: With some refactoring we shouldn't have to dynamically import the required modules for SSR

async function renderToString<P extends PhoriaIslandProps>(
	island: PhoriaIsland<Component>,
	props?: P
) {
	return await Promise.all([import("svelte/server")])
		.then(([SvelteServer]) => {
			const ctx = new Map()
			const html = SvelteServer.render(island.component, {
				props: props as ComponentProps<typeof island.component>,
				context: ctx
			})

			return html.body
		})
}

export { renderToString }
