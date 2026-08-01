import { beforeAll, describe, expect, it } from "vitest"
import { registerComponent, registerSsrService } from "~/register"
import { PhoriaIsland } from "./phoria-island"

describe("PhoriaIsland.create", () => {
	beforeAll(() => {
		registerSsrService("react", {
			render: async () => ({ framework: "react", html: "<div></div>" })
		})

		registerComponent("Counter", {
			framework: "react",
			loader: async () => ({ default: {} })
		})
	})

	it("throws when no component param is present", async () => {
		await expect(PhoriaIsland.create({ params: {}, readProps: async () => undefined })).rejects.toThrow(
			`No "component" was provided in the request path.`
		)
	})

	it("throws when the body is an array rather than an object", async () => {
		await expect(
			PhoriaIsland.create({ params: { component: "Counter" }, readProps: async () => [1, 2] })
		).rejects.toThrow("Props sent in body must be a JSON object.")
	})

	it("treats an absent body as null props", async () => {
		const island = await PhoriaIsland.create({
			params: { component: "Counter" },
			readProps: async () => undefined
		})

		expect(island.props).toBeNull()
	})
})
