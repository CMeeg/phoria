import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5247"

describe("framework-multiple e2e", () => {
	it("serves the home page with rendered island markup", async () => {
		const response = await fetch(webAppUrl)

		expect(response.status).toBe(200)

		const html = await response.text()

		// The index page renders islands; assert the custom element wrapper is present
		expect(html).toContain("phoria-island")
	})

	it("emits modulepreload directives for server-rendered islands", async () => {
		const res = await fetch(webAppUrl)
		const html = await res.text()

		expect(html).toMatch(/<link\s+rel="modulepreload"\s+crossorigin\s+href="\/ui\/assets\/[^"]+\.js">/)
	})
})
