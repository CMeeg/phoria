import { registerFramework, getIslandImport, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { FunctionComponent } from "react"
import { sendHttpResponse, streamHttpResponse } from "./ssr"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server modules - dynamic imports maybe sub-optimal on the server

const frameworkName = "react"

const framework: PhoriaIslandFramework<FunctionComponent> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props, hydrate) => {
				const islandImport = getIslandImport<FunctionComponent>(component)

				Promise.all([import("react"), import("react-dom/client"), islandImport]).then(
					([React, ReactDOM, Island]) => {
						if (hydrate) {
							ReactDOM.hydrateRoot(
								container,
								<React.StrictMode>
									<Island.component {...props} />
								</React.StrictMode>
							)

							return
						}

						const root = ReactDOM.createRoot(container)
						root.render(
							<React.StrictMode>
								<Island.component {...props} />
							</React.StrictMode>
						)
					}
				)
			},
			renderToHttpResponse: async (res, props, options) => {
				const islandImport = getIslandImport<FunctionComponent>(component)

				const island = await islandImport

				res.setHeader("x-phoria-island-framework", frameworkName)

				console.log({ ReactPath: island.componentPath })

				if (island.componentPath) {
					// With Vue, we could also get the component path from `ctx.modules`, but then it'd be inconsistent with other frameworks that don't expose that
					res.setHeader("x-phoria-island-path", island.componentPath)
				}

				if (options?.renderToStream ?? true) {
					await streamHttpResponse(res, island, props, options)
				} else {
					await sendHttpResponse(res, island, props)
				}
			}
		}
	}
}

registerFramework(frameworkName, framework)
