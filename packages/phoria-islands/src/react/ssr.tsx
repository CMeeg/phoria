import type { FunctionComponent } from "react"
import type { HttpResponse, PhoriaIsland, PhoriaIslandRenderOptions } from "~/phoria-island-registry"

const defaultStreamTimeoutMs = 10_000

async function sendHttpResponse<P extends Record<string, unknown> | null>(
	res: HttpResponse,
	island: PhoriaIsland<FunctionComponent>,
	props?: P
) {
	await Promise.all([import("react"), import("react-dom/server")])
		.then(([React, ReactDOM]) => {
			return ReactDOM.renderToString(
				<React.StrictMode>
					<island.component {...props} />
				</React.StrictMode>
			)
		})
		.then((html) => {
			res.status(200).setHeader("Content-Type", "text/html").send(html)
		})
}

async function streamHttpResponse<P extends Record<string, unknown> | null>(
	res: HttpResponse,
	island: PhoriaIsland<FunctionComponent>,
	props?: P,
	options?: PhoriaIslandRenderOptions
) {
	await Promise.all([import("react"), import("react-dom/server"), import("node:stream")]).then(
		([React, ReactDOM, Stream]) => {
			const opts = {
				timeout: defaultStreamTimeoutMs,
				...options
			}

			let didError = false

			const { pipe, abort } = ReactDOM.renderToPipeableStream(
				<React.StrictMode>
					<island.component {...props} />
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

export { sendHttpResponse, streamHttpResponse }
