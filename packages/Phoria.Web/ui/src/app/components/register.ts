import "@phoria/islands/react"
import "@phoria/islands/svelte"
import "@phoria/islands/vue"
import { registerComponents } from "@phoria/islands"

registerComponents({
	ReactCounter: {
		loader: () => import("./Counter/Counter.tsx").then((module) => module.Counter),
		framework: "react"
	},
	VueCounter: {
		loader: () => import("./Counter/Counter.vue").then((module) => module.default),
		framework: "vue"
	},
	SvelteCounter: {
		loader: () => import("./Counter/Counter.svelte").then((module) => module.default),
		framework: "svelte"
	}
})
