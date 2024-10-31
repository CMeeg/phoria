import { registerComponents } from "../../islands/phoria-island-registry"

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
