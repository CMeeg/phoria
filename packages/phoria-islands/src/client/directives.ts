import type { PhoriaIsland } from "./phoria-island"

type PhoriaIslandDirective = (
	mount: () => Promise<void>,
	options: { element: PhoriaIsland; component: string; value: string | null }
) => Promise<void>

const idle: PhoriaIslandDirective = async (mount, { value }) => {
	const cb = async () => {
		await mount()
	}

	const timeout = value ? Number.parseInt(value) : undefined

	if ("requestIdleCallback" in window) {
		window.requestIdleCallback(cb, { timeout })
	} else {
		setTimeout(cb, timeout ?? 200)
	}
}

const visible: PhoriaIslandDirective = async (mount, { element, value }) => {
	const rootMargin = value ?? "0px"

	const observer = new IntersectionObserver(
		async (entries) => {
			for (const entry of entries) {
				if (entry.isIntersecting) {
					observer.disconnect()
					await mount()
				}
			}
		},
		{ rootMargin }
	)

	observer.observe(element)
}

const media: PhoriaIslandDirective = async (mount, { value: query }) => {
	if (!query) {
		throw new Error(`No "query" specified for "client:media" directive.`)
	}

	const mediaQuery = window.matchMedia(query)

	new Promise((resolve) => {
		if (mediaQuery.matches) {
			resolve(true)
		} else {
			mediaQuery.addEventListener("change", resolve, { once: true })
		}
	}).then(() => mount())
}

export { idle, visible, media }

export type { PhoriaIslandDirective }
