import { importComponent } from "@phoria/phoria"
import { type PhoriaIslandComponentCsrService, csrMountMode } from "@phoria/phoria/client"
import type { Component } from "svelte"
import { framework } from "~/main"

const service: PhoriaIslandComponentCsrService<typeof framework.name, Component> = {
	mount: async (island, component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([import("svelte"), importComponent<typeof framework.name, Component>(component)]).then(
			([Svelte, Island]) => {
				// biome-ignore lint/complexity/noBannedTypes: Must match expected props type
				const svelteProps = typeof props === "object" ? (props as {}) : undefined

				if (mode === csrMountMode.hydrate) {
					Svelte.hydrate(Island.component, { target: island, props: svelteProps })
					return
				}

				Svelte.mount(Island.component, { target: island, props: svelteProps })
			}
		)
	}
}

export { service }
