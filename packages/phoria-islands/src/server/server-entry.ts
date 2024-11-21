import { getComponent, getFrameworks } from "~/register"

interface PhoriaServerEntry {
	getComponent: typeof getComponent
	getFrameworks: typeof getFrameworks
}

const serverEntry = {
	getComponent,
	getFrameworks
} satisfies PhoriaServerEntry

export { serverEntry }

export type { PhoriaServerEntry }
