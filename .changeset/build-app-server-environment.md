---
"@phoria/phoria": minor
"@phoria/phoria-react": minor
"@phoria/phoria-svelte": minor
"@phoria/phoria-vue": minor
---

The Phoria Server bundle is now built by the `phoria` plugin as a `server` environment. A separate `vite.server.config.ts` is no longer required. Framework plugins now scope their transform to the client and ssr environments.
