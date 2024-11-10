// TODO: Use h3 instead of express?
import fs from "node:fs/promises"
import express from "express"
import type { ViteDevServer } from "vite"
import type { getComponent as GetComponent, getFrameworks as GetFrameworks } from "@phoria/islands"
// TODO: This path needs to be configurable - perhaps encapsulate most of the server in a function that takes options?
import appsettings from "../../../appsettings.json" with { type: "json" }

const nodeEnv = process.env.NODE_ENV || "development"
const isProduction = nodeEnv === "production"
const port = process.env.PORT || appsettings.Phoria.Server.Port
const base = process.env.BASE || appsettings.Phoria.Base
const ABORT_DELAY = 10000

// Get the SSR manifest if in production mode
// The SSR manifest is produced by the build process
// const ssrManifest = isProduction ? await fs.readFile("./ui/client/.vite/ssr-manifest.json", "utf-8") : undefined

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
		server: {
			middlewareMode: true,
			strictPort: true
		},
		appType: "custom"
	})
	app.use(vite.middlewares)
}

// TODO: Is this necessary?
app.use(express.json())

interface ServerEntry {
	getComponent: GetComponent
	getFrameworks: GetFrameworks
}

const loadServerEntry = async (): Promise<ServerEntry> => {
	if (isProduction) {
		// TODO: This is the build server path - need to make it configurable as it could change
		const entryServerPath = "./ui/server/entry-server.js"
		return (await import(entryServerPath)) as ServerEntry
	}

	if (!vite) {
		throw new Error("Vite dev server is not defined.")
	}

	// TODO: This path needs to be configurable also
	const entryServerPath = "./ui/src/entry-server.tsx"
	// TODO: How can you restart the server if the entry-server.tsx changes? Or if this server.ts file changes?
	return (await vite.ssrLoadModule(entryServerPath)) as ServerEntry
}

// Health check endpoint

app.get("/hc", async (_, res) => {
	const serverEntry = await loadServerEntry()

	res.json({ mode: nodeEnv, frameworks: serverEntry.getFrameworks() })
})

// Ssr endpoint

app.use("/render", async (req, res) => {
	try {
		// TODO: Remove this log (it's for debug), but add some way to configure logging and a logger implementation
		console.log("Rendering", req.originalUrl)

		// Try to get component name from querystring

		const componentName = req.query.component

		if (!componentName) {
			throw new Error(`No "component" name provided in querystring.`)
		}

		const serverEntry = await loadServerEntry()

		const component = serverEntry.getComponent(componentName)

		if (!component) {
			throw new Error(`Component "${componentName}" not found in registry.`)
		}

		// Try get props from body

		const body = req.body

		const props = body !== null && typeof body === "object" && !Array.isArray(body) ? body : null

		// Render the component to the response

		await component.renderToHttpResponse(res, props, { timeout: ABORT_DELAY })
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
