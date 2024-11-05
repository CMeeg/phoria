import { registerFramework, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { Component, ComponentProps } from "svelte"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server functions - dynamic imports maybe sub-optimal on the server

const frameworkName = "svelte"

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props, hydrate) => {
				Promise.all([import("svelte"), component.loader()]).then(([Svelte, Component]) => {
					// biome-ignore lint/complexity/noBannedTypes: Must match expected props type
					const svelteProps = typeof props === "object" ? props as {} : undefined

					if (hydrate) {
						Svelte.hydrate(Component, { target: container, props: svelteProps })
						return
					}

					Svelte.mount(Component, { target: container, props: svelteProps })
				})
			},
			renderToString: async (props) => {
				return Promise.all([import("svelte/server"), component.loader()]).then(([SvelteServer, Component]) => {
					const html = SvelteServer.render(Component, { props: props as ComponentProps<typeof Component> })
					return html.body
				})
			}
		}
	}
}

registerFramework(frameworkName, framework)
