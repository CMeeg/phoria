import { importComponent } from "@phoria/phoria"
import { csrMountMode, type PhoriaIslandComponentCsrService } from "@phoria/phoria/client"
import type { FunctionComponent } from "react"
import { framework } from "~/main"

declare global {
	interface Window {
		$RefreshReg$?: (type: unknown, id: string) => void
		$RefreshSig$?: () => (type: unknown) => unknown
	}
}

const service: PhoriaIslandComponentCsrService<typeof framework.name, FunctionComponent> = {
	mount: async (island, component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		// Phoria serves HTML from the .NET host, so Vite's dev-only react-refresh
		// preamble (normally injected via `transformIndexHtml`) never runs. Define
		// its globals here so `@vitejs/plugin-react`'s refresh transform doesn't
		// throw "can't detect preamble" the first time a component module evaluates.

		if (import.meta.env.DEV) {
			window.$RefreshReg$ ??= () => {}
			window.$RefreshSig$ ??= () => (type) => type
		}

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([
			import("react").then((m) => m.default),
			import("react-dom/client").then((m) => m.default),
			importComponent<typeof framework.name, FunctionComponent>(component)
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
