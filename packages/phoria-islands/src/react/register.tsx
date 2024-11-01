import { registerFramework, type PhoriaIslandFramework } from "~/phoria-island-registry"
import { StrictMode } from "react"
import type { FunctionComponent } from "react"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

const framework: PhoriaIslandFramework<FunctionComponent> = {
	createComponent: (component) => {
		return {
			mountComponent: async (container) => {
				Promise.all([import("react"), import("react-dom/client"), component.loader()]).then(
					([_, ReactDOM, Component]) => {
						const root = ReactDOM.createRoot(container)
						root.render(
							<StrictMode>
								<Component />
							</StrictMode>
						)
					}
				)
			}
		}
	}
}

registerFramework("react", framework)
