import { describe, expect, it, vi } from "vitest"

describe("svelte registration", () => {
	it("registers the client and server services from their main entries", async () => {
		vi.resetModules()
		Object.defineProperty(globalThis, "HTMLElement", { value: class {} })

		const { getCsrService, getSsrService } = await import("@phoria/phoria")
		const csr = await import("./client/csr")
		const ssr = await import("./server/ssr")
		await import("./client/main")
		await import("./server/main")

		expect(getCsrService("svelte")).toBe(csr.service)
		expect(getSsrService("svelte")).toBe(ssr.service)
	})
})
