import { type PhoriaIsland, type PhoriaIslandProps, getComponent, getFrameworks } from "~/register"

interface PhoriaServerEntry {
	getComponent: typeof getComponent
	getFrameworks: typeof getFrameworks
}

const serverEntry = {
	getComponent,
	getFrameworks
} satisfies PhoriaServerEntry

type RenderPhoriaIslandComponent<C, P = PhoriaIslandProps> = (
	island: PhoriaIsland<C>,
	props?: P
) => string | Promise<string | ReadableStream>

export { serverEntry }

export type { PhoriaServerEntry, RenderPhoriaIslandComponent }
