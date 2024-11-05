import { registerFramework, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { Component } from "vue"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server functions - dynamic imports maybe sub-optimal on the server

const frameworkName = "vue"

const framework: PhoriaIslandFramework<Component> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props) => {
				Promise.all([import("vue"), component.loader()]).then(([Vue, Component]) => {
					const app = Vue.createApp(Component, props)
					app.mount(container)
				})
			},
			renderToString: async (props) => {
				return Promise.all([import("vue"), import("vue/server-renderer"), component.loader()])
					.then(([Vue, VueServer, Component]) => {
						const app = Vue.createSSRApp(Component, props)
						const ctx = {}
						return VueServer.renderToString(app, ctx)
					})
					.then((html) => {
						return html
					})
			},
			streamHttpResponse: async (res, props) => {
				const stream = await Promise.all([import("vue"), import("vue/server-renderer"), component.loader()]).then(
					([Vue, VueServer, Component]) => {
						const app = Vue.createSSRApp(Component, props)
						const ctx = {}
						return VueServer.renderToWebStream(app, ctx) as unknown as NodeJS.ReadableStream
					}
				)

				for await (const chunk of stream) {
					if (res.closed) {
						break
					}

					res.write(chunk)
				}

				res.end()
			}
		}
	}
}

registerFramework(frameworkName, framework)
