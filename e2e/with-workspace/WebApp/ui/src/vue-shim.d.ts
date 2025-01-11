declare module "*.vue" {
	// biome-ignore lint/style/useImportType: https://github.com/vuejs/vue-cli/issues/1198#issuecomment-1139752067
	import { defineComponent } from "vue"
	const component: ReturnType<typeof defineComponent>
	export default component
}
