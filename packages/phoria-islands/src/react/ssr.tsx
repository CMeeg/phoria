import type { FunctionComponent } from "react"
import type { PhoriaIsland, PhoriaIslandProps } from "~/phoria-island-registry"

// TODO: With some refactoring we shouldn't have to dynamically import the required modules for SSR

async function renderToString<P extends PhoriaIslandProps>(
	island: PhoriaIsland<FunctionComponent>,
	props?: P
) {
	return await Promise.all([import("react"), import("react-dom/server")])
		.then(([React, ReactDOM]) => {
			return ReactDOM.renderToString(
				<React.StrictMode>
					<island.component {...props} />
				</React.StrictMode>
			)
		})
}

async function renderToStream<P extends PhoriaIslandProps>(
	island: PhoriaIsland<FunctionComponent>,
	props?: P
) {
	// `react-dom/server.edge` is used because of https://github.com/facebook/react/issues/26906
	// Implemented workaround as per https://github.com/redwoodjs/redwood/pull/10284
	// Also having to use React 19 RC because `react-dom/server.edge` is not exported from 18.3.1
	return await Promise.all([import("react"), import("react-dom/server.edge")])
		.then(([React, ReactDOM]) => {
			return ReactDOM.renderToReadableStream(
				<React.StrictMode>
					<island.component {...props} />
				</React.StrictMode>
			)
		})
		.then((stream) => {
			return stream as ReadableStream
		})
}

export { renderToString, renderToStream }
