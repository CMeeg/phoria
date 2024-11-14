import {
	createRouter,
	defineEventHandler,
	getRouterParams,
	createError,
	readBody,
	setResponseHeader,
	useBase
} from "h3"
import type { getComponent, getFrameworks, PhoriaIslandProps } from "~/main"

interface PhoriaServerEntry {
	getComponent: typeof getComponent
	getFrameworks: typeof getFrameworks
}

type PhoriaServerEntryLoader = () => Promise<PhoriaServerEntry> | Promise<PhoriaServerEntry>

function createPhoriaRequestHandler(serverEntry: PhoriaServerEntryLoader, ssrBase?: string) {
	const loadServerEntry = typeof serverEntry === "function" ? serverEntry : () => serverEntry

	const router = createRouter()

	// Health check endpoint

	router.get(
		"/hc",
		defineEventHandler(async () => {
			const serverEntry = await loadServerEntry()

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

			const serverEntry = await loadServerEntry().catch((e) => {
				throw createError({
					status: 500,
					message: "Failed to load server entry.",
					cause: e
				})
			})

			if (typeof serverEntry.getComponent !== "function") {
				throw createError({
					status: 500,
					message: "Server entry does not have a getComponent function."
				})
			}

			const component = serverEntry.getComponent(componentName)

			if (!component) {
				throw createError({
					status: 404,
					message: `Component "${componentName}" not found in registry.`
				})
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
			const result = await component.render(props, { preferStream: true })

			setResponseHeader(event, "x-phoria-island-framework", result.framework)

			if (typeof result.componentPath === "string") {
				setResponseHeader(event, "x-phoria-island-path", result.componentPath)
			}

			return result.html
		})
	)

	const ssrRouterBase = ssrBase ?? "/ssr"
	router.use(`${ssrRouterBase}/**`, useBase(`${ssrRouterBase}`, ssrRouter.handler))

	return router.handler
}

export { createPhoriaRequestHandler }

export type { PhoriaServerEntry, PhoriaServerEntryLoader }
