import {
	createApp,
	createRouter,
	fromNodeMiddleware,
	defineEventHandler,
	createError,
	getRouterParams,
	readBody,
	setResponseHeader,
	useBase,
	toNodeListener
} from "h3"
import { listen } from "listhen"
import { getPhoriaAppSettings } from "@phoria/islands/server"

// Get environment and appsettings

const nodeEnv = process.env.NODE_ENV || "development"
const dotnetEnv = process.env.DOTNET_ENVIRONMENT ?? process.env.ASPNETCORE_ENVIRONMENT ?? nodeEnv
const isProduction = nodeEnv === "production"
const appsettings = await getPhoriaAppSettings(process.cwd(), dotnetEnv)

// Create Vite dev server if not in production environment

const viteDevServer = isProduction
	? undefined
	: await import("vite").then((vite) =>
			vite.createServer({
				server: {
					middlewareMode: true,
					strictPort: true
				},
				appType: "custom"
			})
		)

// Create http server

const app = createApp()

if (viteDevServer) {
	app.use(fromNodeMiddleware(viteDevServer.middlewares))
}

// Handle ssr requests

// TODO: Move this type to the lib
// interface ServerEntry
// {
// 	getComponent: typeof getComponent
// 	getFrameworks: typeof getFrameworks
// }

const devServerEntryPath = "src/entry-server.tsx"
const prodServerEntryPath = "dist/server/entry-server.js"

const loadServerEntry = viteDevServer
	? () => viteDevServer.ssrLoadModule(devServerEntryPath)
	: await import(prodServerEntryPath)

const router = createRouter()

// TODO: Move this to the lib - create a nested "phoria" router
const phoriaRouter = createRouter()

// Health check endpoint
router.get(
	"/hc",
	defineEventHandler(async () => {
		const serverEntry = await loadServerEntry()

		return { mode: nodeEnv, frameworks: serverEntry.getFrameworks() }
	})
)

// Ssr endpoint
phoriaRouter.post(
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
			// TODO: We don't want to move error handling to the lib though so this needs some thought
			// TODO: Take a look into `app.options.onError`
			const error = e instanceof Error ? e : new Error("Unknown error", { cause: e })
			viteDevServer?.ssrFixStacktrace(error)

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

// TODO: Maybe just return the handler from the lib?
const ssrBase = appsettings.Ssr?.Base ?? "/ssr"
router.use(`${ssrBase}/**`, useBase(`${ssrBase}`, phoriaRouter.handler))

app.use(router.handler)

// TODO: This was used by `sirv` (static file server) - still needed in h3?
// const base = appsettings.Phoria.Base

const port = appsettings.Server?.Port ?? 5173

// TODO: How do we put this in watch mode when in dev?
const listener = await listen(toNodeListener(app), {
	port,
	isProd: isProduction
})

export { app, listener }
