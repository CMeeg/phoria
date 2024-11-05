// TODO: Use h3 instead of express?
import fs from "node:fs/promises"
import express from "express"
import type { ViteDevServer } from "vite"
import type { getComponent as GetComponent } from "@phoria/islands"
// TODO: These paths need to be configurable - perhaps encapsulate most of the server in a function that takes options?
import appsettingsDev from "../../../appsettings.Development.json" with { type: "json" }
import appsettings from "../../../appsettings.json" with { type: "json" }

const isProduction = process.env.NODE_ENV === "production"
const port = process.env.PORT || appsettingsDev.Vite.Server.Port
const base = process.env.BASE || `/${appsettings.Vite.Base}`
const ABORT_DELAY = 10000

// Get the SSR manifest if in production mode
// The SSR manifest is produced by the build process
const ssrManifest = isProduction ? await fs.readFile("./ui/client/.vite/ssr-manifest.json", "utf-8") : undefined

// Create http server
const app = express()

// Add Vite or respective production middlewares
let vite: ViteDevServer | undefined
if (isProduction) {
	// TODO: What does compression do? Is it necessary?
	const compression = (await import("compression")).default
	// TODO: What does sirv do? Is it necessary?
	const sirv = (await import("sirv")).default
	app.use(compression())
	app.use(base, sirv("./client", { extensions: [] }))
} else {
	const { createServer } = await import("vite")
	vite = await createServer({
		server: { middlewareMode: true },
		appType: "custom"
	})
	app.use(vite.middlewares)
}

// TODO: Is this necessary?
app.use(express.json())

// Serve HTML
// TODO: This is a catch all route - prob want a specific route for islands
// TODO: Also have a /health route for health checks on startup
app.use("*", async (req, res) => {
	try {
		// TODO: Is this replace necessary?
		const url = req.originalUrl.replace(base, "")
		// TODO: Remove this log (it's for debug), but add some way to configure logging and a logger implementation
		console.log("Rendering", url)

		const qs = new URLSearchParams(url)

		// Try to get component name from querystring

		const componentName = qs.get("component")

		if (!componentName) {
			throw new Error(`No "component" name provided in querystring.`)
		}

		// Try get props from body

		const body = req.body

		const props = body !== null && typeof body === "object" && !Array.isArray(body) ? body : null

		// TODO: Do we need to export this function or is it enough to just import the entry-server file?
		let getComponent: GetComponent

		if (isProduction) {
			// TODO: This is the build server path - need to make it configurable as it could change
			const entryServerPath = "./ui/server/entry-server.js"
			getComponent = (await import(entryServerPath)).getComponent
		} else {
			if (!vite) {
				throw new Error("Vite dev server is not defined.")
			}

			// TODO: This path needs to be configurable also
			const entryServerPath = "./ui/src/entry-server.tsx"
			// TODO: How can you restart the server if the entry-server.tsx changes? Or if this server.ts file changes?
			getComponent = (await vite.ssrLoadModule(entryServerPath)).getComponent
		}

		const component = getComponent(componentName)

		if (!component) {
			throw new Error(`Component "${componentName}" not found in registry.`)
		}

		// TODO: This demonstrates passing data to the caller though so maybe useful, but might remove if not needed
		res.setHeader("x-phoria-component-framework", component.framework)

		// TODO: Svelte also returns a `head` property - need to look into that, and if anything simlar can be done with React and Vue (maybe using the ssrManifest?)
		if (typeof component.streamHttpResponse === "function") {
			await component.streamHttpResponse(res, props, { timeout: ABORT_DELAY })
		} else {
			// TODO: Maybe make symmetrical with streamHttpResponse i.e. pass response in to function, or let framework handle the choice of whether to stream or not
			const html = await component.renderToString(props)
			res.status(200).setHeader("Content-Type", "text/html").send(html)
		}
	} catch (e: unknown) {
		const error = e instanceof Error ? e : new Error("Unknown error", { cause: e })
		vite?.ssrFixStacktrace(error)
		console.log(error.stack)
		res.status(500).end(error.stack)
	}
})

// Start http server
// TODO: https?
app.listen(port, () => {
	console.log(`Server started at http://localhost:${port}`)
})
