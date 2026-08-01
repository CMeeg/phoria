import { readFile, stat } from "node:fs/promises"
import { basename, join } from "node:path"
import { pathToFileURL } from "node:url"
import {
	createError,
	createRouter,
	defineEventHandler,
	type EventHandler,
	type EventHandlerRequest,
	fromNodeMiddleware,
	getRouterParams,
	readBody,
	serveStatic,
	setResponseHeader,
	useBase
} from "h3"
import mime from "mime/lite"
import { isRunnableDevEnvironment, type ViteDevServer } from "vite"
import { getFrameworks } from "~/register"
import type { PhoriaAppSettings } from "./appsettings"
import { PhoriaIsland } from "./phoria-island"
import type { PhoriaServerEntry } from "./ssr"

/**
 * A request handler that can be mounted on a Phoria Server app.
 *
 * The underlying HTTP library is an implementation detail of Phoria and may
 * change in a future minor release. Treat values of this type as opaque.
 */
type PhoriaRequestHandler = EventHandler<EventHandlerRequest, unknown>

function isServerEntry(serverEntry: unknown): serverEntry is PhoriaServerEntry {
	if (typeof serverEntry === "undefined" || serverEntry === null) {
		return false
	}

	if (typeof serverEntry !== "object") {
		return false
	}

	if (!("renderPhoriaIsland" in serverEntry)) {
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
			const serverEntry = await loadServerEntry()

			if (!isServerEntry(serverEntry)) {
				throw createError({
					status: 500,
					message: "Server entry is not of type `PhoriaServerEntry`."
				})
			}

			const nodeEnv = process.env.NODE_ENV ?? "development"

			return { mode: nodeEnv, frameworks: getFrameworks() }
		})
	)

	// Ssr endpoint

	const ssrRouter = createRouter()

	const renderRoutePath = "/render/:component"

	ssrRouter.post(
		renderRoutePath,
		defineEventHandler(async (event) => {
			const serverEntry = await loadServerEntry()

			if (!isServerEntry(serverEntry)) {
				throw createError({
					status: 500,
					message: "Server entry is not of type `PhoriaServerEntry`."
				})
			}

			try {
				const phoriaIsland = await PhoriaIsland.create({
					params: getRouterParams(event),
					readProps: () => readBody(event)
				})

				const result = await serverEntry.renderPhoriaIsland(phoriaIsland)

				setResponseHeader(event, "x-phoria-island-framework", result.framework)

				if (typeof result.componentPath === "string") {
					setResponseHeader(event, "x-phoria-island-path", result.componentPath)
				}

				return result.html
			} catch (error) {
				throw createError({
					status: 500,
					message: "Error rendering component.",
					cause: error
				})
			}
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
): PhoriaRequestHandler {
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

function createPhoriaDevSsrRequestHandler(
	viteDevServer: ViteDevServer,
	appsettings: PhoriaAppSettings
): PhoriaRequestHandler {
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
): PhoriaRequestHandler {
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

				if (!stats?.isFile()) {
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

function createPhoriaDevCsrRequestHandler(viteDevServer: ViteDevServer): PhoriaRequestHandler {
	return fromNodeMiddleware(viteDevServer.middlewares)
}

export type { PhoriaRequestHandler, PhoriaServerEntry, PhoriaServerEntryLoader }
export {
	createPhoriaCsrRequestHandler,
	createPhoriaDevCsrRequestHandler,
	createPhoriaDevSsrRequestHandler,
	createPhoriaSsrRequestHandler
}
