import {
	createRouter,
	defineEventHandler,
	getRouterParams,
	createError,
	readBody,
	setResponseHeader,
	useBase,
	serveStatic
} from "h3"
import { stat, readFile } from "node:fs/promises"
import { join } from "node:path"
import mime from "mime/lite"
import { getSsrService, type getComponent, type getFrameworks, type PhoriaIslandProps } from "~/register"

interface PhoriaServerEntry {
	getComponent: typeof getComponent
	getFrameworks: typeof getFrameworks
}

type PhoriaServerEntryLoader = () => Promise<Record<string, unknown>> | PhoriaServerEntry

function createPhoriaSsrRequestHandler(serverEntry: PhoriaServerEntryLoader, base: string) {
	const loadServerEntry = typeof serverEntry === "function" ? serverEntry : () => serverEntry

	const router = createRouter()

	// Health check endpoint

	router.get(
		"/hc",
		defineEventHandler(async () => {
			const serverEntry = await loadServerEntry()

			if (typeof serverEntry.getFrameworks !== "function") {
				throw createError({
					status: 500,
					message: "Server entry does not export a getFrameworks function."
				})
			}

			const nodeEnv = process.env.NODE_ENV || "development"

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

			// TODO: Could validate params with https://h3.unjs.io/examples/validate-data#validate-params, but it's only one param so maybe not worth it?
			const componentName = params.component

			if (!componentName) {
				throw createError({
					status: 400,
					message: `No "component" was provided in the request path. Please make the request to ${renderRoutePath}.`
				})
			}

			const serverEntry = await loadServerEntry()

			if (typeof serverEntry.getComponent !== "function") {
				throw createError({
					status: 500,
					message: "Server entry does not export a getComponent function."
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
				// TODO: Validate props with https://h3.unjs.io/examples/validate-data#validate-body - props could be anything though so not sure how useful this would be
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

	return router.handler
}

function createPhoriaCsrRequestHandler(base: string, cwd: string) {
	const basePath = base.startsWith("/") ? base.substring(1) : base
	const publicDir = "phoria/client"

	const staticFilehandler = defineEventHandler((event) => {
		return serveStatic(event, {
			getContents: (id) => {
				const filePath = join(cwd, basePath, publicDir, id)

				return readFile(filePath)
			},
			getMeta: async (id) => {
				const filePath = join(cwd, basePath, publicDir, id)

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

	return createRouter().use(`${base}/**`, useBase(base, staticFilehandler)).handler
}

export { createPhoriaSsrRequestHandler, createPhoriaCsrRequestHandler }

export type { PhoriaServerEntry, PhoriaServerEntryLoader }
