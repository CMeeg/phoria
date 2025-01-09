import type { PhoriaIslandComponent, PhoriaIslandComponentModule, PhoriaIslandProps } from "~/phoria-island"

type PhoriaIslandCsrMountMode = keyof typeof csrMountMode

const csrMountMode = {
	render: "render",
	hydrate: "hydrate"
} as const

interface PhoriaIslandCsrOptions {
	mode: PhoriaIslandCsrMountMode
}

interface PhoriaIslandComponentCsrService<T> {
	mount: (
		island: HTMLElement,
		component: PhoriaIslandComponent<PhoriaIslandComponentModule, T>,
		props: PhoriaIslandProps,
		options?: Partial<PhoriaIslandCsrOptions>
	) => Promise<void>
}

export { csrMountMode }

export type { PhoriaIslandCsrMountMode, PhoriaIslandCsrOptions, PhoriaIslandComponentCsrService }
