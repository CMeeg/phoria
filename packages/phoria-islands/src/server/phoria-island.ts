import type { PhoriaIslandComponentEntry, PhoriaIslandComponentModule, PhoriaIslandProps } from "~/phoria-island"
import { getComponent, getSsrService } from "~/register"
import type { PhoriaIslandComponentSsrService, RenderPhoriaIslandComponentOptions } from "./ssr"

/** The request data `PhoriaIsland.create` needs, independent of any HTTP library. */
interface PhoriaIslandRequest {
	params: Record<string, string | undefined>
	readProps: () => Promise<unknown>
}

class PhoriaIsland<F extends string = string, C = unknown, P extends PhoriaIslandProps = PhoriaIslandProps> {
	private component: PhoriaIslandComponentEntry<F, PhoriaIslandComponentModule, C>
	private ssr: PhoriaIslandComponentSsrService<F, C>

	componentName: string
	props: P
	framework: F

	constructor(
		component: PhoriaIslandComponentEntry<F, PhoriaIslandComponentModule, C>,
		props: P,
		ssr: PhoriaIslandComponentSsrService<F, C>
	) {
		this.component = component
		this.ssr = ssr

		this.componentName = component.name
		this.props = props
		this.framework = component.framework
	}

	async render(options?: Partial<RenderPhoriaIslandComponentOptions<F, C>>) {
		return await this.ssr.render(this.component, this.props, options)
	}

	static async create(request: PhoriaIslandRequest) {
		// Try to get the component to render

		const componentName = request.params.component

		if (!componentName) {
			throw new Error(`No "component" was provided in the request path.`)
		}

		const component = getComponent(componentName)

		if (!component) {
			throw new Error(`Component "${componentName}" not found in registry.`)
		}

		// Try to get the SSR service to use to render the component

		const ssr = getSsrService(component.framework)

		if (typeof ssr === "undefined") {
			throw new Error(`No SSR service could be found for framework "${component.framework}".`)
		}

		// Try get props from body

		let props: PhoriaIslandProps = null

		const body = await request.readProps()

		if (typeof body !== "undefined" && body !== null) {
			if (typeof body !== "object" || Array.isArray(body)) {
				throw new Error("Props sent in body must be a JSON object.")
			}

			props = body as Record<string, unknown>
		}

		return new PhoriaIsland(component, props, ssr)
	}
}

export type { PhoriaIslandRequest }
export { PhoriaIsland }
