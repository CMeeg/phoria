<script lang="ts">
const base = import.meta.env.BASE_URL

// biome-ignore lint/style/useConst: Svelte
let { startAt = 0 } = $props()

// biome-ignore lint/style/useConst: Svelte
let count = $state(startAt)
</script>

<div class="svelte-counter">
	<a href="https://svelte.dev" target="_blank" rel="noreferrer">
		<img src="{base}/svelte.svg" class="logo svelte" alt="Svelte Logo" />
	</a>
	<button type="button" onclick={() => count++}>count is {count}</button>
	<p>
		Edit
		<code>ui/src/components/Counter/Counter.svelte</code> to test HMR
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
