import { importComponent } from "@phoria/phoria"
import { type PhoriaIslandComponentCsrService, csrMountMode } from "@phoria/phoria/client"
import type { FunctionComponent } from "react"
import { framework } from "~/main"

const service: PhoriaIslandComponentCsrService<FunctionComponent> = {
	mount: async (island, component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([
			import("react").then((m) => m.default),
			import("react-dom/client").then((m) => m.default),
			importComponent<FunctionComponent>(component)
		]).then(([React, ReactDOM, Island]) => {
			if (mode === csrMountMode.hydrate) {
				ReactDOM.hydrateRoot(
					island,
					<React.StrictMode>
						<Island.component {...props} />
					</React.StrictMode>
				)

				return
			}

			const root = ReactDOM.createRoot(island)
			root.render(
				<React.StrictMode>
					<Island.component {...props} />
				</React.StrictMode>
			)
		})
	}
}

export { service }
