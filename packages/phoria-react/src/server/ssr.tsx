import { type PhoriaIslandProps, importComponent } from "@phoria/phoria"
import type { PhoriaIslandComponentSsrService, RenderPhoriaIslandComponent } from "@phoria/phoria/server"
import { type FunctionComponent, StrictMode } from "react"
import { renderToString } from "react-dom/server"
import { renderToReadableStream } from "react-dom/server.edge"
import { framework } from "~/main"

type RenderReactPhoriaIslandComponent<P extends PhoriaIslandProps = PhoriaIslandProps> = RenderPhoriaIslandComponent<
	FunctionComponent,
	P
>

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

const service: PhoriaIslandComponentSsrService<FunctionComponent> = {
	render: async (component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		// TODO: Can "cache" the imported component? Maybe only in production?
		const island = await importComponent<FunctionComponent>(component)

		const renderComponent = options?.renderComponent ?? renderComponentToStream

		const html = await renderComponent(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}

export { service, renderComponentToStream, renderComponentToString }

export type { RenderReactPhoriaIslandComponent }
