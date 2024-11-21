import { getComponent, getCsrService, csrMountMode, type PhoriaIslandProps } from "~/register"

class PhoriaIsland extends HTMLElement {
	async connectedCallback() {
		// TODO: Add support for "directives" like Astro - see https://github.com/bholmesdev/vite-conf-islands-arch/blob/main/src/client.ts

		const componentName = this.getAttribute("component")

		if (!componentName) {
			throw new Error(`No "component" attribute specified on <phoria-island> element.`)
		}

		try {
			const component = getComponent(componentName)

			if (typeof component === "undefined") {
				throw new Error(`No component found with name "${componentName}".`)
			}

			const csr = getCsrService(component.framework)

			if (typeof csr === "undefined") {
				throw new Error(`No CSR service could be found for framework "${component.framework}".`)
			}

			const rawProps = this.getAttribute("props")
			let props: PhoriaIslandProps = null

			if (typeof rawProps === "string") {
				props = JSON.parse(rawProps)
			}

			const mode = this.hasAttribute("client:only") ? csrMountMode.render : csrMountMode.hydrate

			await csr.mount(this, component, props, { mode })
		} catch (error) {
			console.error(`Error loading "${componentName}" component:`, error)
		}
	}

	static register() {
		if ("customElements" in window) {
			customElements.define("phoria-island", PhoriaIsland)
		}
	}
}

export { PhoriaIsland }
