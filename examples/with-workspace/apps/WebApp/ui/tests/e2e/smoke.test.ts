import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5673"

async function getHtml(path = "") {
	const response = await fetch(`${webAppUrl}${path}`)
	expect(response.status).toBe(200)
	return await response.text()
}

describe("with-workspace e2e", () => {
	it("serves the home page with the shared counter island", async () => {
		const html = await getHtml()
		expect(html).toContain("phoria-island")
		expect(html).toContain("react-counter")
		expect(html).toContain("count is 5")
	})

	it("emits modulepreload directives for server-rendered islands", async () => {
		const html = await getHtml()
		expect(html).toMatch(/<link\s+rel="modulepreload"\s+crossorigin\s+href="\/ui\/assets\/[^"]+\.js">/)
	})

	it("reports the Phoria server healthy", async () => {
		const response = await fetch(`${webAppUrl}/health`)
		expect(response.status).toBe(200)
		expect(await response.text()).toContain("Healthy")
	})
})
