import { registerFramework, type PhoriaIslandFramework } from "../phoria-island-registry"
import type { Component } from "vue"

// TODO: The "framework registrations" should be handled by "plugins"

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			mountComponent: async (container) => {
				Promise.all([import("vue"), component.loader()]).then(([Vue, Component]) => {
					const app = Vue.createApp(Component)
					app.mount(container)
				})
			}
		}
	}
}

registerFramework("vue", framework)
