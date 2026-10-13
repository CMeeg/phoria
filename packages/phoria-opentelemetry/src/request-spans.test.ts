import { context, trace } from "@opentelemetry/api"
import { createApp, createRouter, type H3Event, setResponseHeader, toWebHandler } from "h3"
import { afterAll, beforeAll, describe, expect, it, vi } from "vitest"
import { createPhoriaOtelAppSettings } from "../tests/utilities/otel-appsettings-fixture"
import { createPhoriaRequestSpanHook, withPhoriaOtelInstrumentation } from "./request-spans"

let observability: { shutdown: () => Promise<void> }

beforeAll(async () => {
	const { createPhoriaObservability } = await import("./observability")
	observability = createPhoriaObservability(
		createPhoriaOtelAppSettings({ observability: { tracing: { enabled: true, samplingRatio: 1 } } })
	)
})

afterAll(async () => {
	await observability.shutdown()
})

describe("createPhoriaRequestSpanHook", () => {
	it("returns onRequest and onBeforeResponse hooks", () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })

		expect(typeof hook.onRequest).toBe("function")
		expect(typeof hook.onBeforeResponse).toBe("function")
	})

	it("is a no-op for onBeforeResponse when no span is stashed", () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })
		const event = { context: {}, method: "GET", path: "/ui/asset.js" } as H3Event

		expect(() => hook.onBeforeResponse(event)).not.toThrow()
	})

	it("renames only CSR assets under the configured base path", () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })
		const updateName = vi.fn()

		for (const path of ["/uifoo", "/ui/asset.js"]) {
			const event = { context: { phoriaSpan: { updateName } }, method: "GET", path } as unknown as H3Event
			hook.onBeforeResponse(event)
		}

		expect(updateName).toHaveBeenCalledTimes(1)
		expect(updateName).toHaveBeenCalledWith("phoria-server.csr.asset")
	})

	it("creates request hooks from parsed appsettings", () => {
		const hook = withPhoriaOtelInstrumentation({ base: "/assets", ssrBase: "/render" })
		const updateName = vi.fn()
		const event = {
			context: { phoriaSpan: { updateName } },
			method: "GET",
			path: "/assets/app.js"
		} as unknown as H3Event

		hook.onBeforeResponse(event)

		expect(updateName).toHaveBeenCalledWith("phoria-server.csr.asset")
	})

	it("records SSR request attributes from a real H3 request", async () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })
		const span = { updateName: vi.fn(), setAttribute: vi.fn() }
		const app = createApp({ onRequest: hook.onRequest, onBeforeResponse: hook.onBeforeResponse })
		const router = createRouter()
		router.post("/ssr/render/Counter", async (event) => {
			setResponseHeader(event, "x-phoria-island-framework", "react")
			return "ok"
		})
		app.use(router.handler)

		await context.with(trace.setSpan(context.active(), span as never), () =>
			toWebHandler(app)(new Request("http://localhost/ssr/render/Counter", { method: "POST" }), {})
		)

		expect(span.updateName).toHaveBeenCalledWith("phoria-server.ssr.render")
		expect(span.setAttribute).toHaveBeenCalledWith("phoria.component", "Counter")
		expect(span.setAttribute).toHaveBeenCalledWith("phoria.framework", "react")
	})

	it("records CSR asset requests from a real H3 request", async () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })
		const span = { updateName: vi.fn(), setAttribute: vi.fn() }
		const app = createApp({ onRequest: hook.onRequest, onBeforeResponse: hook.onBeforeResponse })
		const router = createRouter()
		router.get("/ui/assets/app.js", async (_event) => {
			return "ok"
		})
		app.use(router.handler)

		await context.with(trace.setSpan(context.active(), span as never), () =>
			toWebHandler(app)(new Request("http://localhost/ui/assets/app.js"), {})
		)

		expect(span.updateName).toHaveBeenCalledWith("phoria-server.csr.asset")
	})

	it("does not touch a span when no active span exists", async () => {
		const hook = createPhoriaRequestSpanHook({ base: "/ui", ssrBase: "/ssr" })
		let eventContext: H3Event["context"] | undefined
		const app = createApp({ onRequest: hook.onRequest, onBeforeResponse: hook.onBeforeResponse })
		const router = createRouter()
		router.get("/ui/assets/app.js", async (event) => {
			eventContext = event.context
			return "ok"
		})
		app.use(router.handler)

		await toWebHandler(app)(new Request("http://localhost/ui/assets/app.js"), {})

		expect(eventContext?.phoriaSpan).toBeUndefined()
	})
})
