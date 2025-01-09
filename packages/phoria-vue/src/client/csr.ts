import { createIslandImport } from "@phoria/phoria"
import type { PhoriaIslandComponentCsrService } from "@phoria/phoria/client"
import type { Component } from "vue"

const service: PhoriaIslandComponentCsrService<Component> = {
	mount: async (island, component, props) => {
		const islandImport = createIslandImport<Component>(component)

		Promise.all([import("vue"), islandImport]).then(([Vue, Island]) => {
			const app = Vue.createApp(Island.component, props)
			app.mount(island)
		})
	}
}

export { service }
