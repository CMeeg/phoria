---
"@phoria/phoria": minor
---

`PhoriaIsland.create` now takes a `PhoriaIslandRequest` instead of an h3 `H3Event`. Request handler factories (`createPhoriaSsrRequestHandler`, `createPhoriaDevSsrRequestHandler`, `createPhoriaCsrRequestHandler`, `createPhoriaDevCsrRequestHandler`) return the new `PhoriaRequestHandler` type.
