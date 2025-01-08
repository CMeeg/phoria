import type { PhoriaIslandSsrResult } from "~/register"
import type { PhoriaIsland } from "./phoria-island"

interface PhoriaServerEntry {
	renderPhoriaIsland: (phoriaIsland: PhoriaIsland<unknown>) => Promise<PhoriaIslandSsrResult>
}

export type { PhoriaServerEntry }
