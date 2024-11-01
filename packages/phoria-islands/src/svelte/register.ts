import { registerFramework, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { Component } from "svelte"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			mountComponent: async (container) => {
				Promise.all([import("svelte"), component.loader()]).then(([Svelte, Component]) => {
					Svelte.mount(Component, { target: container })
				})
			}
		}
	}
}

registerFramework("svelte", framework)
