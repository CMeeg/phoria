import { importComponent } from "@phoria/phoria"
import type { PhoriaIslandComponentCsrService } from "@phoria/phoria/client"
import type { Component } from "vue"
import { framework } from "~/main"

const service: PhoriaIslandComponentCsrService<typeof framework.name, Component> = {
	mount: async (island, component, props) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		Promise.all([import("vue"), importComponent<typeof framework.name, Component>(component)]).then(([Vue, Island]) => {
			const app = Vue.createApp(Island.component, props)
			app.mount(island)
		})
	}
}

export { service }
