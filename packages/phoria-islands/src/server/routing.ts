import { readFile, stat } from "node:fs/promises"
import { basename, join } from "node:path"
import { pathToFileURL } from "node:url"
import {
	createError,
	createRouter,
	defineEventHandler,
	fromNodeMiddleware,
	getRouterParams,
	readBody,
	serveStatic,
	setResponseHeader,
	useBase
} from "h3"
import mime from "mime/lite"
import type { ViteDevServer, DevEnvironment, RunnableDevEnvironment } from "vite"
import { type PhoriaIslandProps, getSsrService } from "~/register"
import type { PhoriaAppSettings } from "./appsettings"
import type { PhoriaServerEntry } from "./server-entry"

function isServerEntry(serverEntry: unknown): serverEntry is PhoriaServerEntry {
	if (typeof serverEntry === "undefined" || serverEntry === null) {
		return false
	}

	if (typeof serverEntry !== "object") {
		return false
	}

	if (!("getComponent" in serverEntry)) {
		return false
	}

	if (!("getFrameworks" in serverEntry)) {
		return false
	}

	return true
}

type PhoriaServerEntryLoader = () => Promise<Record<string, unknown>>

function createPhoriaSsrRouter(loadServerEntry: PhoriaServerEntryLoader, base: string) {
	const router = createRouter()

	// Health check endpoint

	router.get(
		"/hc",
		defineEventHandler(async () => {
			const { serverEntry } = await loadServerEntry()

			if (!isServerEntry(serverEntry)) {
				throw createError({
					status: 500,
					message:
						"Server entry does not have a named export `serverEntry` or export is not of type `PhoriaServerEntry`."
				})
			}

			const nodeEnv = process.env.NODE_ENV ?? "development"

			return { mode: nodeEnv, frameworks: serverEntry.getFrameworks() }
		})
	)

	// Ssr endpoint

	const ssrRouter = createRouter()

	const renderRoutePath = "/render/:component"

	ssrRouter.post(
		renderRoutePath,
		defineEventHandler(async (event) => {
			const params = getRouterParams(event)

			// Try to get the component to render

			const componentName = params.component

			if (!componentName) {
				throw createError({
					status: 400,
					message: `No "component" was provided in the request path. Please make the request to ${renderRoutePath}.`
				})
			}
			const { serverEntry } = await loadServerEntry()

			if (!isServerEntry(serverEntry)) {
				throw createError({
					status: 500,
					message:
						"Server entry does not have a named export `serverEntry` or export is not of type `PhoriaServerEntry`."
				})
			}

			const component = serverEntry.getComponent(componentName)

			if (!component) {
				throw createError({
					status: 404,
					message: `Component "${componentName}" not found in registry.`
				})
			}

			// Try to get the SSR service to use to render the component

			const ssr = getSsrService(component.framework)

			if (typeof ssr === "undefined") {
				throw new Error(`No SSR service could be found for framework "${component.framework}".`)
			}

			// Try get props from body

			let props: PhoriaIslandProps = null

			const body = await readBody(event)

			if (typeof body !== "undefined" && body !== null) {
				if (typeof body !== "object" || Array.isArray(body)) {
					throw createError({
						status: 400,
						message: "Props sent in body must be a JSON object."
					})
				}

				props = body
			}

			// Render the component to the response

			// TODO: Expose a way to set `preferStream` from the request?
			const result = await ssr.render(component, props, { preferStream: true })

			setResponseHeader(event, "x-phoria-island-framework", result.framework)

			if (typeof result.componentPath === "string") {
				setResponseHeader(event, "x-phoria-island-path", result.componentPath)
			}

			return result.html
		})
	)

	router.use(`${base}/**`, useBase(base, ssrRouter.handler))

	return router
}

interface PhoriaSsrRequestHandlerOptions {
	cwd: string
}

const defaultSsrRequestHandlerOptions: PhoriaSsrRequestHandlerOptions = {
	cwd: process.cwd()
}

function createPhoriaSsrRequestHandler(
	appsettings: PhoriaAppSettings,
	options?: Partial<PhoriaSsrRequestHandlerOptions>
) {
	const opts = { ...defaultSsrRequestHandlerOptions, ...options }

	// Without `pathToFileURL` you will receive a `ERR_UNSUPPORTED_ESM_URL_SCHEME` error on Windows
	const ssrEntry = pathToFileURL(
		join(
			opts.cwd,
			appsettings.root,
			appsettings.build.outDir,
			"phoria",
			"ssr",
			basename(appsettings.ssrEntry).replaceAll(".ts", ".js")
		)
	).href

	const ssrRouter = createPhoriaSsrRouter(() => import(ssrEntry), appsettings.ssrBase)

	return ssrRouter.handler
}

function isRunnableDevEnvironment(
	environment: DevEnvironment | RunnableDevEnvironment
): environment is RunnableDevEnvironment {
	return "runner" in environment
}

function createPhoriaDevSsrRequestHandler(viteDevServer: ViteDevServer, appsettings: PhoriaAppSettings) {
	const environment = viteDevServer.environments.ssr

	if (!isRunnableDevEnvironment(environment)) {
		throw new Error("Vite dev server does not have a runnable SSR environment.")
	}

	const ssrRouter = createPhoriaSsrRouter(() => environment.runner.import(appsettings.ssrEntry), appsettings.ssrBase)

	return ssrRouter.handler
}

interface PhoriaCsrRequestHandlerOptions {
	cwd: string
}

const defaultCsrRequestHandlerOptions: PhoriaCsrRequestHandlerOptions = {
	cwd: process.cwd()
}

function createPhoriaCsrRequestHandler(
	appsettings: PhoriaAppSettings,
	options?: Partial<PhoriaCsrRequestHandlerOptions>
) {
	const opts = { ...defaultCsrRequestHandlerOptions, ...options }

	const staticFilehandler = defineEventHandler((event) => {
		return serveStatic(event, {
			getContents: (id) => {
				const filePath = join(opts.cwd, appsettings.root, appsettings.build.outDir, "phoria", "client", id)

				return readFile(filePath)
			},
			getMeta: async (id) => {
				const filePath = join(opts.cwd, appsettings.root, appsettings.build.outDir, "phoria", "client", id)

				const stats = await stat(filePath).catch(() => {})

				if (!stats || !stats.isFile()) {
					return
				}

				return {
					size: stats.size,
					mtime: stats.mtimeMs,
					type: mime.getType(filePath) ?? "application/octet-stream"
					// TODO: Encoding?
					// encoding: "TODO"
				}
			}
		})
	})

	const base = appsettings.base

	return createRouter().use(`${base}/**`, useBase(base, staticFilehandler)).handler
}

function createPhoriaDevCsrRequestHandler(viteDevServer: ViteDevServer) {
	return fromNodeMiddleware(viteDevServer.middlewares)
}

export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler
}

export type { PhoriaServerEntry, PhoriaServerEntryLoader }
