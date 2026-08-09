import type { H3Event } from "h3"
import { describe, expect, it } from "vitest"
import { createPhoriaRequestSpanHook } from "./request-spans"

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
})
