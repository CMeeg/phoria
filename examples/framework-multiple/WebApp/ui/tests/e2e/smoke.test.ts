import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5573"

async function getHtml(path = "") {
  const response = await fetch(`${webAppUrl}${path}`)
  expect(response.status).toBe(200)
  return await response.text()
}

describe("framework-multiple e2e", () => {
  it("serves the home page with rendered island markup", async () => {
    const html = await getHtml()
    expect(html).toContain("phoria-island")
  })

  it("emits modulepreload directives for server-rendered islands", async () => {
    const html = await getHtml()
    expect(html).toMatch(/<link\s+rel="modulepreload"\s+crossorigin\s+href="\/ui\/assets\/[^"]+\.js">/)
  })

  it("server-renders all three framework counters", async () => {
    const html = await getHtml()
    expect(html).toContain("react-counter")
    expect(html).toContain("vue-counter")
    expect(html).toContain("svelte-counter")
  })

  it("renders the factory-based islands (ViewComponent and TagHelper)", async () => {
    const html = await getHtml()
    expect(html).toContain("count is 9")
    expect(html).toContain("count is 19")
  })

  it("filters islands by the ?framework= query parameter", async () => {
    const html = await getHtml("?framework=react")
    expect(html).toContain("react-counter")
    expect(html).not.toContain("vue-counter")
    expect(html).not.toContain("svelte-counter")
  })
})
