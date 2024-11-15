import { createIslandImport, csrMountMode, type PhoriaIslandComponentCsrService } from "@meeg/phoria"
import type { Component } from "svelte"

const service: PhoriaIslandComponentCsrService = {
	mount: async (island, component, props, options) => {
		const islandImport = createIslandImport<Component>(component)

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([import("svelte"), islandImport]).then(([Svelte, Island]) => {
			// biome-ignore lint/complexity/noBannedTypes: Must match expected props type
			const svelteProps = typeof props === "object" ? (props as {}) : undefined

			if (mode === csrMountMode.hydrate) {
				Svelte.hydrate(Island.component, { target: island, props: svelteProps })
				return
			}

			Svelte.mount(Island.component, { target: island, props: svelteProps })
		})
	}
}

export { service }
