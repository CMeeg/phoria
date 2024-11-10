import { registerFramework, getIslandImport, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { Component } from "vue"
import { sendHttpResponse, streamHttpResponse } from "./ssr"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server modules - dynamic imports maybe sub-optimal on the server

const frameworkName = "vue"

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props) => {
				const islandImport = getIslandImport<Component>(component)

				Promise.all([import("vue"), islandImport]).then(([Vue, Island]) => {
					const app = Vue.createApp(Island.component, props)
					app.mount(container)
				})
			},
			renderToHttpResponse: async (res, props, options) => {
				const islandImport = getIslandImport<Component>(component)

				const island = await islandImport

				res.setHeader("x-phoria-island-framework", frameworkName)

				if (island.componentPath) {
					// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
					res.setHeader("x-phoria-island-path", island.componentPath)
				}

				if (options?.renderToStream ?? true) {
					await streamHttpResponse(res, island, props)
				} else {
					await sendHttpResponse(res, island, props)
				}
			}
		}
	}
}

registerFramework(frameworkName, framework)
