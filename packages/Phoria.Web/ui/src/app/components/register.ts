import "@phoria/islands/react"
import "@phoria/islands/svelte"
import "@phoria/islands/vue"
import { registerComponents } from "@phoria/islands"

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
