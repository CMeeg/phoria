import { getComponent, getCsrService, csrMountMode, type PhoriaIslandProps } from "~/register"
import { idle, visible, media, type PhoriaIslandDirective } from "./directives"

const directives = new Map<string, PhoriaIslandDirective>()
directives.set("client:load", async (mount) => await mount())
directives.set("client:idle", idle)
directives.set("client:visible", visible)
directives.set("client:media", media)

class PhoriaIsland extends HTMLElement {
	async connectedCallback() {
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

			// This is a special case because it does not hydrate on mount and has highest priority

			if (this.hasAttribute("client:only")) {
				await csr.mount(this, component, props, { mode: csrMountMode.render })

				return
			}

			// Otherwise we check for a client directive to determine the hydration strategy

			for (const [name, directive] of directives) {
				if (!this.hasAttribute(name)) {
					continue
				}

				const value = this.getAttribute(name)

				await directive(() => csr.mount(this, component, props, { mode: csrMountMode.hydrate }), {
					element: this,
					component: componentName,
					value
				})

				return
			}

			// No directives found

			throw new Error("No known client directive was found.")
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
