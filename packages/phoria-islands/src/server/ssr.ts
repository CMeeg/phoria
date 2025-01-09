import type { PhoriaIsland, PhoriaIslandComponent, PhoriaIslandComponentModule, PhoriaIslandProps } from "~/phoria-island"
// TODO: Evidently I have too many types named `PhoriaIsland` :(
import type { PhoriaIsland as SsrPhoriaIsland } from "./phoria-island"

interface PhoriaIslandSsrResult {
	framework: string
	componentPath?: string
	html: string | ReadableStream
}

interface PhoriaIslandComponentSsrService<T> {
	render: (
		component: PhoriaIslandComponent<PhoriaIslandComponentModule, T>,
		props: PhoriaIslandProps,
		options?: Partial<RenderPhoriaIslandComponentOptions<T>>
	) => Promise<PhoriaIslandSsrResult>
}

type RenderPhoriaIslandComponent<C, P = PhoriaIslandProps> = (
	island: PhoriaIsland<C>,
	props?: P
) => string | Promise<string | ReadableStream>

interface RenderPhoriaIslandComponentOptions<C> {
	renderComponent: RenderPhoriaIslandComponent<C>
}

interface PhoriaServerEntry {
	renderPhoriaIsland: (phoriaIsland: SsrPhoriaIsland<unknown>) => Promise<PhoriaIslandSsrResult>
}

export type {
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
