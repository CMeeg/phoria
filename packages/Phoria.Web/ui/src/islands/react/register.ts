import { registerFramework, type PhoriaIslandFramework } from "../phoria-island-registry"
import type { FunctionComponent } from "react"

// TODO: The "framework registrations" should be handled by "plugins"

const framework: PhoriaIslandFramework<FunctionComponent> = {
	createComponent: (component) => {
		return {
			mountComponent: async (container) => {
				Promise.all([import("react"), import("react-dom/client"), component.loader()]).then(
					([React, ReactDOM, Component]) => {
						// TODO: StrictMode?
						const root = ReactDOM.createRoot(container)
						root.render(React.createElement(Component))
					}
				)
			}
		}
	}
}

registerFramework("react", framework)
