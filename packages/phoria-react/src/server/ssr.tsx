import { type PhoriaIslandComponentSsrService, createIslandImport } from "@phoria/phoria"
import type { PhoriaIslandSsrRender } from "@phoria/phoria/server"
import { type FunctionComponent, StrictMode } from "react"
import { renderToString as reactRenderToString } from "react-dom/server"
import { renderToReadableStream } from "react-dom/server.edge"
import { framework } from "~/main"

const renderIslandToString: PhoriaIslandSsrRender<FunctionComponent> = (island, props) => {
	return reactRenderToString(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

const renderIslandToStream: PhoriaIslandSsrRender<FunctionComponent> = async (island, props) => {
	// `react-dom/server.edge` is used because of https://github.com/facebook/react/issues/26906
	// Implemented workaround as per https://github.com/redwoodjs/redwood/pull/10284
	return await renderToReadableStream(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

// TODO: Align naming convention with other frameworks
interface ReactSsrOptions {
	renderIsland: PhoriaIslandSsrRender<FunctionComponent>
}

const ssrOptions: ReactSsrOptions = {
	renderIsland: renderIslandToStream
}

function configureReactSsr(options: Partial<ReactSsrOptions>) {
	if (typeof options.renderIsland !== "undefined") {
		ssrOptions.renderIsland = options.renderIsland
	}
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<FunctionComponent>(component)
		const island = await islandImport

		const html = await ssrOptions.renderIsland(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, configureReactSsr, renderIslandToStream, renderIslandToString }
