import type {
	PhoriaIslandComponent,
	PhoriaIslandComponentEntry,
	PhoriaIslandComponentModule,
	PhoriaIslandProps
} from "~/phoria-island"
import type { PhoriaIsland } from "./phoria-island"

interface PhoriaIslandSsrResult {
	framework: string
	componentPath?: string
	html: string | ReadableStream
}

interface PhoriaIslandComponentSsrService<F extends string, T> {
	render: (
		component: PhoriaIslandComponentEntry<F, PhoriaIslandComponentModule, T>,
		props: PhoriaIslandProps,
		options?: Partial<RenderPhoriaIslandComponentOptions<F, T>>
	) => Promise<PhoriaIslandSsrResult>
}

type RenderPhoriaIslandComponent<F extends string, C, P extends PhoriaIslandProps = PhoriaIslandProps> = (
	island: PhoriaIslandComponent<F, C>,
	props?: P
) => string | Promise<string | ReadableStream>

interface RenderPhoriaIslandComponentOptions<F extends string, C> {
	renderComponent: RenderPhoriaIslandComponent<F, C>
}

interface PhoriaServerEntry {
	renderPhoriaIsland: (island: PhoriaIsland) => Promise<PhoriaIslandSsrResult>
}

export type {
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
