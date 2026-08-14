import { beforeEach, describe, expect, it, vi } from "vitest"

describe("PhoriaIsland.create", () => {
	beforeEach(() => {
		vi.resetModules()
	})

	it("throws when no component param is present", async () => {
		const { PhoriaIsland } = await import("./phoria-island")

		await expect(PhoriaIsland.create({ params: {}, readProps: async () => undefined })).rejects.toThrow(
			`No "component" was provided in the request path.`
		)
	})

	it("throws when the body is an array rather than an object", async () => {
		const { registerSsrComponentFramework } = await import("../../tests/utilities/register-fakes")
		const { PhoriaIsland } = await import("./phoria-island")

		registerSsrComponentFramework()

		await expect(
			PhoriaIsland.create({ params: { component: "Counter" }, readProps: async () => [1, 2] })
		).rejects.toThrow("Props sent in body must be a JSON object.")
	})

	it("treats an absent body as null props", async () => {
		const { registerSsrComponentFramework } = await import("../../tests/utilities/register-fakes")
		const { PhoriaIsland } = await import("./phoria-island")

		registerSsrComponentFramework()

		const island = await PhoriaIsland.create({
			params: { component: "Counter" },
			readProps: async () => undefined
		})

		expect(island.props).toBeNull()
	})
})
