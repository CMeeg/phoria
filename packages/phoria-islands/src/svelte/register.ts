import { registerFramework, getIslandImport, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { Component } from "svelte"
import { sendHttpResponse } from "./ssr"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server modules - dynamic imports maybe sub-optimal on the server

const frameworkName = "svelte"

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props, hydrate) => {
				const islandImport = getIslandImport<Component>(component)

				Promise.all([import("svelte"), islandImport]).then(([Svelte, Island]) => {
					// biome-ignore lint/complexity/noBannedTypes: Must match expected props type
					const svelteProps = typeof props === "object" ? (props as {}) : undefined

					if (hydrate) {
						Svelte.hydrate(Island.component, { target: container, props: svelteProps })
						return
					}

					Svelte.mount(Island.component, { target: container, props: svelteProps })
				})
			},
			renderToHttpResponse: async (res, props) => {
				const islandImport = getIslandImport<Component>(component)

				const island = await islandImport

				res.setHeader("x-phoria-island-framework", frameworkName)

				if (island.componentPath) {
					// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
					res.setHeader("x-phoria-island-path", island.componentPath)
				}

				// TODO: Svelte doesn't support streaming - should I acknowledge that somehow?
				await sendHttpResponse(res, island, props)
			}
		}
	}
}

registerFramework(frameworkName, framework)
