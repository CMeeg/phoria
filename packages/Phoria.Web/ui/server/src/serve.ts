import {
	createApp,
	createRouter,
	fromNodeMiddleware,
	defineEventHandler,
	createError,
	getRouterParams,
	readBody,
	setResponseHeader,
	toNodeListener
} from "h3"
import { listen } from "listhen"
import type { ViteDevServer } from "vite"
import type { getComponent, getFrameworks } from "@phoria/islands"
// TODO: This path needs to be configurable - perhaps encapsulate most of the server in a function that takes options?
// Could maybe use the lib in the recent js perf update to "find closest"
import appsettings from "../../../appsettings.json" with { type: "json" }

const nodeEnv = process.env.NODE_ENV || "development"
const isProduction = nodeEnv === "production"
const port = process.env.PORT || appsettings.Phoria.Server.Port
// TODO: This was used by `sirv` (static file server) - still needed in h3?
// const base = process.env.BASE || appsettings.Phoria.Base
const ABORT_DELAY = 10000

// Create http server
const app = createApp()

// Add middlewares
let vite: ViteDevServer | undefined
if (!isProduction) {
	const { createServer } = await import("vite")

	vite = await createServer({
		server: {
			middlewareMode: true,
			strictPort: true
		},
		appType: "custom"
	})

	app.use(fromNodeMiddleware(vite.middlewares))
}

interface ServerEntry {
	getComponent: typeof getComponent
	getFrameworks: typeof getFrameworks
}

// TODO: I think because of things like this, some of the server needs to remain in the consuming project - I may just need to move the internal/critical/boilerplatey things to a library - look at Remix for inspiration
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

// Define routes

const router = createRouter()

// Health check endpoint
router.get(
	"/hc",
	defineEventHandler(async () => {
		const serverEntry = await loadServerEntry()

		return { mode: nodeEnv, frameworks: serverEntry.getFrameworks() }
	})
)

// Ssr endpoint
router.post(
	"/render/:component",
	defineEventHandler(async (event) => {
		const params = getRouterParams(event)

		try {
			// Try to get the component to render

			// TODO: Could validate params with https://h3.unjs.io/examples/validate-data#validate-params, but it's only one param so maybe not worth it?
			const componentName = params.component

			if (!componentName) {
				throw new Error(
					`No "component" name provided in path. Please make the request to /render/:component where :component is the name of the component to render.`
				)
			}

			const serverEntry = await loadServerEntry()

			const component = serverEntry.getComponent(componentName)

			if (!component) {
				throw new Error(`Component "${componentName}" not found in registry.`)
			}

			// Try get props from body

			const body = await readBody(event)

			// TODO: Validate props with https://h3.unjs.io/examples/validate-data#validate-body - props could be anything though so not sure how useful this would be
			const props = body !== null && typeof body === "object" && !Array.isArray(body) ? body : null

			// Render the component to the response

			const result = await component.render(props, { preferStream: true })

			setResponseHeader(event, "x-phoria-island-framework", result.framework)

			if (result.componentPath) {
				setResponseHeader(event, "x-phoria-island-path", result.componentPath)
			}

			return result.html
		} catch (e: unknown) {
			const error = e instanceof Error ? e : new Error("Unknown error", { cause: e })
			vite?.ssrFixStacktrace(error)

			// TODO: Prob want to log these via some logging service, but need to leave this up to the consuming project
			console.log(error.stack)

			throw createError({
				status: 500,
				message: error.message,
				fatal: true
			})
		}
	})
)

app.use(router)

// TODO: How do we put this in watch mode when in dev?
const listener = await listen(toNodeListener(app), {
	port,
	isProd: isProduction
})

export { app, listener }
