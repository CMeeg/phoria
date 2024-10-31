import { getComponent } from "./phoria-island-registry"

class PhoriaIsland extends HTMLElement {
	async connectedCallback() {
		// TODO: Add support for "directives" like Astro - see https://github.com/bholmesdev/vite-conf-islands-arch/blob/main/src/client.ts

		const componentName = this.getAttribute("component")

		if (!componentName) {
			throw new Error(`No "component" attribute specified on <phoria-island> element.`)
		}

		try {
			const component = getComponent(componentName)

			if (!component) {
				throw new Error(`Component "${componentName}" not found in registry.`)
			}

			await component.mountComponent(this)
		} catch (error) {
			// TODO: Error handling needs to be customisable from the caller
			console.error(`Error loading "${componentName}" component:`, error)
		}
	}
}

customElements.define("phoria-island", PhoriaIsland)
