<script setup lang="ts">
import { ref } from "vue"

const props = defineProps<{ startAt?: number }>()

const count = ref(props.startAt ?? 0)
</script>

<template>
	<div class="vue-counter">
		<a href="https://vuejs.org/" target="_blank" rel="noreferrer">
			<img src="/vue.svg" class="logo vue" alt="Vue logo" />
		</a>
		<button type="button" @click="count++">count is {{ count }}</button>
		<p>
			Edit
			<code>ui/app/components/Counter/Counter.vue</code> to test HMR
		</p>
	</div>
</template>

<style scoped>
.logo.vue:hover {
	filter: drop-shadow(0 0 2em #42b883aa);
}

button {
	display: block;
	margin: auto;
}
</style>
