import { registerFramework, type PhoriaIslandFramework } from "../phoria-island-registry"
import type { Component } from "svelte"

// TODO: The "framework registrations" should be handled by "plugins"

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
