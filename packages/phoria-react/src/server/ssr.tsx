import { type PhoriaIslandComponentSsrService, type PhoriaIslandProps, createIslandImport } from "@phoria/phoria"
import type { RenderPhoriaIslandComponent } from "@phoria/phoria/server"
import { type FunctionComponent, StrictMode } from "react"
import { renderToString } from "react-dom/server"
import { renderToReadableStream } from "react-dom/server.edge"
import { framework } from "~/main"

type RenderReactPhoriaIslandComponent<P = PhoriaIslandProps> = RenderPhoriaIslandComponent<FunctionComponent, P>

const renderComponentToString: RenderReactPhoriaIslandComponent = (island, props) => {
	return renderToString(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

const renderComponentToStream: RenderReactPhoriaIslandComponent = async (island, props) => {
	// `react-dom/server.edge` is used because of https://github.com/facebook/react/issues/26906
	// Implemented workaround as per https://github.com/redwoodjs/redwood/pull/10284
	return await renderToReadableStream(
		<StrictMode>
			<island.component {...props} />
		</StrictMode>
	)
}

interface PhoriaReactSsrOptions {
	renderComponent: RenderReactPhoriaIslandComponent
}

const ssrOptions: PhoriaReactSsrOptions = {
	renderComponent: renderComponentToStream
}

function configureReactSsrService(options: Partial<PhoriaReactSsrOptions>) {
	if (typeof options.renderComponent !== "undefined") {
		ssrOptions.renderComponent = options.renderComponent
	}
}

const service: PhoriaIslandComponentSsrService = {
	render: async (component, props) => {
		// TODO: Can "cache" the imported component? Maybe only in production?
		const islandImport = createIslandImport<FunctionComponent>(component)
		const island = await islandImport

		const html = await ssrOptions.renderComponent(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, configureReactSsrService, renderComponentToStream, renderComponentToString }

export type { PhoriaReactSsrOptions, RenderReactPhoriaIslandComponent }
