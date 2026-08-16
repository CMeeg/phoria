import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5773"

async function getHtml(path = "") {
  const response = await fetch(`${webAppUrl}${path}`)
  expect(response.status).toBe(200)
  return await response.text()
}

describe("with-tailwind e2e", () => {
  it("serves the home page with rendered island markup", async () => {
    const html = await getHtml()
    expect(html).toContain("phoria-island")
  })

  it("emits modulepreload directives for server-rendered islands", async () => {
    const html = await getHtml()
    expect(html).toMatch(/<link\s+rel="modulepreload"\s+crossorigin\s+href="\/ui\/assets\/[^"]+\.js">/)
  })

  it("server-renders the React counter", async () => {
    const html = await getHtml()
    expect(html).toContain("react-counter")
    expect(html).toContain("motion-safe:animate-spin-slow")
    expect(html).toContain("block mx-auto my-6")
  })

  it("renders Tailwind utility classes from Razor markup", async () => {
    const html = await getHtml()
    expect(html).toContain("bg-neutral-50")
  })

  it("reports the Phoria server healthy via the health check", async () => {
    const response = await fetch(`${webAppUrl}/health`)

    expect(response.status).toBe(200)
    expect(await response.text()).toContain("Healthy")
  })
})
