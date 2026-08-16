---
"@phoria/phoria": minor
"@phoria/phoria-react": minor
"@phoria/phoria-svelte": minor
"@phoria/phoria-vue": minor
---

The Phoria Server bundle is now built by the `phoria` plugin as a `server` environment. A separate `vite.server.config.ts` is no longer required. Framework plugins now scope their entire per-environment plugin instance (via `applyToEnvironment`) to the client and ssr environments — not just their `transform` hook, though `transform` is currently the only per-environment hook these plugins define.
