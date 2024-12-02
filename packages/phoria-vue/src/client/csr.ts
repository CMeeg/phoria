import { type PhoriaIslandComponentCsrService, createIslandImport } from "@phoria/phoria"
import type { Component } from "vue"

const service: PhoriaIslandComponentCsrService = {
	mount: async (island, component, props) => {
		const islandImport = createIslandImport<Component>(component)

		Promise.all([import("vue"), islandImport]).then(([Vue, Island]) => {
			const app = Vue.createApp(Island.component, props)
			app.mount(island)
		})
	}
}

export { service }
