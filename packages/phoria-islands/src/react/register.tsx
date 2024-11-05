import { registerFramework, type PhoriaIslandFramework } from "~/phoria-island-registry"
import type { FunctionComponent } from "react"

// TODO: Split into a separate package?
// TODO: "Registration" is done automatically on import - should this be up to the caller?
// TODO: Is it worth exploring a "plugin" system for this or using Vite's plugin system?

// TODO: Maybe split into client and server functions - dynamic imports maybe sub-optimal on the server

const frameworkName = "react"

const defaultStreamTimeoutMs = 10_000

const framework: PhoriaIslandFramework<FunctionComponent> = {
	createComponent: (component) => {
		return {
			framework: frameworkName,
			mount: async (container, props, hydrate) => {
				Promise.all([import("react"), import("react-dom/client"), component.loader()]).then(
					([React, ReactDOM, Component]) => {
						if (hydrate) {
							ReactDOM.hydrateRoot(
								container,
								<React.StrictMode>
									<Component {...props} />
								</React.StrictMode>
							)

							return
						}

						const root = ReactDOM.createRoot(container)
						root.render(
							<React.StrictMode>
								<Component {...props} />
							</React.StrictMode>
						)
					}
				)
			},
			renderToString: async (props) => {
				return Promise.all([import("react"), import("react-dom/server"), component.loader()]).then(
					([React, ReactDOM, Component]) => {
						return ReactDOM.renderToString(
							<React.StrictMode>
								<Component {...props} />
							</React.StrictMode>
						)
					}
				)
			},
			streamHttpResponse: async (res, props, options) => {
				Promise.all([import("react"), import("react-dom/server"), import("node:stream"), component.loader()]).then(
					([React, ReactDOM, Stream, Component]) => {
						const opts = {
							timeout: defaultStreamTimeoutMs,
							...options
						}

						let didError = false

						const { pipe, abort } = ReactDOM.renderToPipeableStream(
							<React.StrictMode>
								<Component {...props} />
							</React.StrictMode>,
							{
								onShellReady() {
									res.status(didError ? 500 : 200)
									res.setHeader("Content-Type", "text/html")

									const transformStream = new Stream.Transform({
										transform(chunk, encoding, callback) {
											res.write(chunk, encoding)
											callback()
										}
									})

									transformStream.on("finish", () => {
										res.end()
									})

									pipe(transformStream)
								},
								onShellError() {
									res.status(500)
									res.setHeader("Content-Type", "text/html")
									res.send("<h1>Something went wrong</h1>")
								},
								onError(error: unknown) {
									didError = true
									// TODO: Pass through logger from caller
									console.error(error)
								}
							}
						)

						setTimeout(() => {
							// TODO: Pass through logger from caller
							console.log("Aborting render")
							abort()
						}, opts.timeout)
					}
				)
			}
		}
	}
}

registerFramework(frameworkName, framework)
