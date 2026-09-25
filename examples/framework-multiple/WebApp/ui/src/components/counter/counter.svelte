<script lang="ts">
// biome-ignore lint/correctness/noUnusedVariables: used in template
const base = import.meta.env.BASE_URL

let { startAt = 0 } = $props()

// startAt intentionally seeds independent local state; later prop changes should not reset count
// svelte-ignore state_referenced_locally
// biome-ignore lint/correctness/noUnusedVariables: used in template
let count = $state(startAt)
</script>

<div class="svelte-counter">
	<a href="https://svelte.dev" target="_blank" rel="noreferrer">
		<img src="{base}/svelte.svg" class="logo svelte" alt="Svelte Logo" />
	</a>
	<button type="button" onclick={() => count++}>count is {count}</button>
	<p>
		Edit
		<code>ui/src/components/counter/counter.svelte</code> to test HMR
	</p>
</div>

<style>
.logo.svelte:hover {
	filter: drop-shadow(0 0 2em #ff3e00aa);
}

button {
	display: block;
	margin: auto;
}
</style>
