# Task 8 Report: `e2e/with-sidecar`

## Implementation

- Added the React-only `e2e/with-sidecar` workspace package and lockfile importer.
- Added a net10 WebApp with Razor Pages, one React SSR/client island, Vite production output, and Phoria middleware.
- Added an Aspire 13.4.6 AppHost with exactly one application resource: `WebApp`.
- Kept Node out of the AppHost resource graph. `PhoriaServerProcess` starts `node ui/dist/server/server.js` from the WebApp content root.
- Added Production configuration with `DOTNET_ENVIRONMENT=Production` and `NODE_ENV=production`.
- Applied the Task 7 OTel logging pattern to .NET and Node. `WithOtlpExporter` supplies the WebApp OTLP environment, which the child inherits unchanged.
- Used a TypeScript `.ts` SSR wrapper because the Phoria production router maps `.ts` to the generated `.js` SSR entry. The requested `.tsx` entry remains the implementation entry.
- No published package files were modified.

## Verification

All commands were run from `e2e/with-sidecar`:

- `pnpm build`: passed. Vite client, SSR, and Node server bundles built; WebApp Release build passed.
- `pnpm check`: passed.
- `pnpm lint`: passed.
- `dotnet build WebApp/WebApp.csproj --configuration Release`: passed with 0 warnings and 0 errors.
- `dotnet build Phoria.AppHost/Phoria.AppHost.csproj --configuration Release`: passed with 0 warnings and 0 errors.
- `pnpm preview`: launched Aspire CLI 13.4.6 and the dashboard.
- Preview request to `http://127.0.0.1:5248/`: returned the SSR-rendered React counter island.
- Preview process tree was AppHost -> WebApp -> `node ui/dist/server/server.js`; no Node sibling resource was registered.
- Child environment inspection confirmed `NODE_ENV=production`, `DOTNET_ENVIRONMENT=Production`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_RESOURCE_ATTRIBUTES`, and `OTEL_SERVICE_NAME`.
- Normal Aspire stop exited cleanly and left no AppHost, WebApp, or Node process.
- Direct Production WebApp normal stop captured `PhoriaServerProcess` event 1215: `Phoria server process 427197 was sent a termination signal`, followed by event 1212 process exit; no child remained.
- Controlled force stop used `SIGSTOP` on the Node child, then stopped the WebApp. The log captured event 1216: the child did not exit within the grace period and was forcefully terminated; no child remained.

## Concerns

- Aspire certificate trust is partially failed in this environment, but the HTTP app endpoint, dashboard, and OTLP endpoint started successfully.
- The Node shutdown OTel records were not emitted into the captured parent console before host shutdown; the .NET termination-signal and force-stop logs provide the lifecycle evidence.
