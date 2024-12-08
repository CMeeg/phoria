import {
	type PhoriaIsland,
	type PhoriaIslandComponentSsrService,
	type PhoriaIslandProps,
	createIslandImport
} from "@phoria/phoria"
import { type FunctionComponent, StrictMode } from "react"
import { renderToString as reactRenderToString } from "react-dom/server"
import { renderToReadableStream } from "react-dom/server.edge"
import { framework } from "~/main"

async function renderToString<P extends PhoriaIslandProps>(island: PhoriaIsland<FunctionComponent>, props?: P) {
	return reactRenderToString(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

async function renderToStream<P extends PhoriaIslandProps>(island: PhoriaIsland<FunctionComponent>, props?: P) {
	// `react-dom/server.edge` is used because of https://github.com/facebook/react/issues/26906
	// Implemented workaround as per https://github.com/redwoodjs/redwood/pull/10284
	return await renderToReadableStream(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props, options) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<FunctionComponent>(component)
		const island = await islandImport

		const html =
			(options?.preferStream ?? true) ? await renderToStream(island, props) : await renderToString(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service }
