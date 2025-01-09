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

interface PhoriaIslandComponentSsrService<T> {
	render: (
		component: PhoriaIslandComponentEntry<PhoriaIslandComponentModule, T>,
		props: PhoriaIslandProps,
		options?: Partial<RenderPhoriaIslandComponentOptions<T>>
	) => Promise<PhoriaIslandSsrResult>
}

type RenderPhoriaIslandComponent<C, P extends PhoriaIslandProps = PhoriaIslandProps> = (
	island: PhoriaIslandComponent<C>,
	props?: P
) => string | Promise<string | ReadableStream>

interface RenderPhoriaIslandComponentOptions<C> {
	renderComponent: RenderPhoriaIslandComponent<C>
}

interface PhoriaServerEntry {
	renderPhoriaIsland: (island: PhoriaIsland<unknown>) => Promise<PhoriaIslandSsrResult>
}

export type {
	PhoriaIslandComponentSsrService,
	PhoriaIslandSsrResult,
	PhoriaServerEntry,
	RenderPhoriaIslandComponent,
	RenderPhoriaIslandComponentOptions
}
