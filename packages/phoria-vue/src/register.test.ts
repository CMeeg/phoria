import { describe, expect, it, vi } from "vitest"

describe("vue registration", () => {
	it("registers the client and server services from their main entries", async () => {
		vi.resetModules()

		const { getCsrService, getSsrService } = await import("@phoria/phoria")
		const csr = await import("./client/csr")
		const ssr = await import("./server/ssr")
		await import("./client/main")
		await import("./server/main")

		expect(getCsrService("vue")).toBe(csr.service)
		expect(getSsrService("vue")).toBe(ssr.service)
	})
})
