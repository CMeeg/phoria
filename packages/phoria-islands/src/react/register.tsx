import { registerFramework, getIslandImport, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { FunctionComponent } from "react"
import { renderToString, renderToStream } from "./ssr"

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

				Promise.all([import("react"), import("react-dom/client"), islandImport]).then(([React, ReactDOM, Island]) => {
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
				})
			},
			render: async (props, options) => {
				// TODO: Can "cache" the imported component? Maybe only in production?
				const islandImport = getIslandImport<FunctionComponent>(component)
				const island = await islandImport

				const html = options?.preferStream ?? true
					? await renderToStream(island, props)
					: await renderToString(island, props)

				return {
					framework: frameworkName,
					componentPath: island.componentPath,
					html
				}
			}
		}
	}
}

registerFramework(frameworkName, framework)
