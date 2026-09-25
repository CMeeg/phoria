import { registerComponent, registerCsrService, registerSsrService } from "../../src/register"

// IMPORTANT: consumers must call `vi.resetModules()` and then dynamically
// `import("../../tests/utilities/register-fakes")` alongside their own dynamic import of the module under
// test. A static top-level import would capture a stale module registry across resets and break the shared
// registry.

export function registerSsrComponentFramework(name = "react", html = "<div></div>", componentPath?: string) {
	registerSsrService(name, { render: async () => ({ framework: name, html, componentPath }) })
	registerComponent("Counter", { framework: name, loader: async () => ({ default: {} }) })
}

export function registerCsrFramework(name = "test") {
	registerCsrService(name, { mount: async () => {} })
}
