import { registerComponents } from "@meeg/phoria"

registerComponents({
	ReactCounter: {
		loader: {
			module: () => import("./Counter/Counter.tsx"),
			component: (module) => module.Counter
		},
		framework: "react"
	},
	VueCounter: {
		loader: () => import("./Counter/Counter.vue"),
		framework: "vue"
	},
	SvelteCounter: {
		loader: () => import("./Counter/Counter.svelte"),
		framework: "svelte"
	}
})
