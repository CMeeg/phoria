import type { H3Event } from "h3"
import { describe, expect, it, vi } from "vitest"
import { createPhoriaRequestSpanHook, withPhoriaOtelInstrumentation } from "./request-spans"

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
})
