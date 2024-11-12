import { getComponent, type PhoriaIslandProps } from "./main"

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

			const rawProps = this.getAttribute("props")
			let props: PhoriaIslandProps = null
			if (rawProps !== null) {
				props = JSON.parse(rawProps || "{}")
			}

			// TODO: Boolean may not be the best way to handle this
			const hydrate = !this.hasAttribute("client:only")

			await component.mount(this, props, hydrate)
		} catch (error) {
			// TODO: Error handling needs to be customisable from the caller
			console.error(`Error loading "${componentName}" component:`, error)
		}
	}

	static register(tagName?: string) {
		if ("customElements" in window) {
			customElements.define(tagName || "phoria-island", PhoriaIsland)
		}
	}
}

export { PhoriaIsland }
