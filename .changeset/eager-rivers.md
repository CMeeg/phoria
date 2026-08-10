---
"@phoria/opentelemetry": patch
---

Fix Phoria Server request metrics (`http.server.request.duration`) missing in production by forcing the HTTP/HTTPS instrumentation patch to apply to the already-loaded core modules.
