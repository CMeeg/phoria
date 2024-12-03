import { type PhoriaIslandComponentCsrService, createIslandImport, csrMountMode } from "@phoria/phoria"
import type { FunctionComponent } from "react"

const service: PhoriaIslandComponentCsrService = {
	mount: async (island, component, props, options) => {
		const islandImport = createIslandImport<FunctionComponent>(component)

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([import("react"), import("react-dom/client"), islandImport]).then(([React, ReactDOM, Island]) => {
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
