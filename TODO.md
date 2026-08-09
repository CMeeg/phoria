# TODO

* Testing - Add unit tests
* Testing - Add Playwright to example test suites
* Review and update deps - any new features of interest?
* Vite - bundling of dotnet assets
* Components - support for composition i.e. nested components
* Server - smooth off rough edges like the background service not shutting the vite server down
* Islands - support for streaming e.g. suspense
* Islands - support for server actions
* Islands - add a web components library
* Server - adapters for Deno etc
* Server - memory pools in dotnet 10 https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-9.0#manage-memory-pools

## Notes

### Phase 1.5

* [ ] Only the React package has tests - Svelte and Vue do not
* [ ] Can / should tests be added to cover the server/ssr implementations in the framework packages?
* [ ] More generally, are there any other holes in the test coverage?
* [x] Replace lerna?

### Phase 2

* [x] Aspire?
* [x] Has `npm-run-all` now been replaced with `concurrently` everywhere?
* [x] `InternalsVisibleTo` code smell in tests - is there a better approach?
* [x] I was never very happy with usage of `LoggerMessage` and in particular `EventId` - it seems like a maintenance burden and easy to get wrong - suggested improvements?
* [-] Is there anything that can be done about the debugger shutdown issue in the Phoria server?
* [x] Explain to me the usage of `DllImport("libc" ...` in `PhoriaServerProcess` and why that is needed?
* [x] There was the following feedback from the agent, "Final review found one remaining Important issue: PhoriaServerProcess can dereference a nullified periodicTimer during shutdown, causing NullReferenceException (PhoriaServerProcess.cs:100)." - can we take care of that?
* [x] In the Node server I'd like to be able to pass a logger into the CSR and SSR handlers so they can log too - maybe we need to expose some kind of interface so that the library doesn't need to take a dependency on otel
* [x] Can logging fall back to console.log if otel has not been configured?
* [x] The former sidecar example now uses a Preview environment consistently with the other example flows
* [x] The former workspace example's Aspire AppHost was moved to the app root so sibling resources can be colocated there
* [x] The former sidecar and multi-framework example build scripts were aligned where their ownership models allow
* [x] In the `dev:aspire` scripts the environment is passed like `--environment Development`, but I was under the impression that `aspire run` always ran as the "Development" environment - if so, should this be removed as it's redundant?
* [x] The Aspire `--environment` flag should be used to set `DOTNET_ENVIRONMENT` and `ASPNETCORE_ENVIRONMENT` dynamically in the app host as [described here](https://aspire.dev/deployment/environments/#set-environment-variables-on-child-resources) - we should also use it to set `NODE_ENV` as per the example - if `IsDevelopment()` is true then the env vars should be set to `development`, otherwise they should be set to whatever the environment name provided is
* [x] The e2e apphost program files specifically load the `appsettings.Preview.json`, but the actual file(s) loaded should follow regular dotnet conventions (i.e. appsettings.json plus any current environment specific file, if available) - please can you update to not hardcode this specific file. Can you also check if loading these files in the app host is really needed as the apps should read them themselves automatically - having the app host read them feels like a code smell now I think about it!
* [x] Overriding appsettings in the multi-framework example was removed in favor of normal environment-specific configuration
* [x] Does `launchSettings.json` still get used under Aspire? If so, do these need to be added/updated in the e2e apps? If not, then should they be updated/removed to avoid confusion and maintenance overhead? It has utility when not running the app by Aspire so I don't think it needs to go entirely, but I want to make sure it is accurate
* [x] The "building for production" guide needs to be updated to use `aspire start` - should it also include the `stop` script as well as the `preview` script?
* [x] It looks like some comments added in code have been added in reference to sequencing or decisions by why tasks were undertaken and don't relate to the code itself - for example, the comment in `EventId` about `EventFeature was replaced...` - can you remove these comments or replace with something actually useful (if there is anything useful to add) and update `AGENTS.md` to clarify when and when not to add comments
* [x] Some docs like the "getting started" guide have been updated to reference specific versions of tools/libraries e.g. Aspire 13.4.6 - I would rather not have specific versions referenced unless it is absolutely necessary as it creates docs maintenance churn - can you review and remove explicit version numbers in doc updates unless they are absolutely necessary
* [x] I thought `.superpowers` directory was git ignored, but there are files in this PR added in this dir - do these need to be removed and gitignore checked? Are there other files from this dir to be removed from the repo? I'm not bothered about cleaning Git history if so, but want to tidy up now and going forward.

* [x] Added opt-in traces and metrics to the Phoria server and the web app.


* [ ] Can you review tests added on this branch to make sure that there is a high signal to low noise ratio - I am concerned that some tests were added to cover development tasks / issues that may not provide a lot of value going forwards - especially around testing the dotnet Phoria Server implementation
* [ ] Can you provide a critical review of `PhoriaServerMonitorService` and `PhoriaServerStatus` tracking - does this provide a robust implementation that will provide monitoring and error recovery of the sidecar node process during the lifetime of the dotnet app process? Use the Microsoft Learn docs MCP to help with research

* [ ] `examples/framework-multiple/WebApp/ui/src/components/Counter/Counter.svelte`: review the Svelte `state_referenced_locally` warning.

transforming...11:12:01 PM [vite-plugin-svelte] examples/framework-multiple/WebApp/ui/src/components/Counter/Counter.svelte:8:19 This reference only captures the initial value of `startAt`. Did you mean to reference it inside a closure instead?
[islands] https://svelte.dev/e/state_referenced_locally
[islands]  6:
[islands]  7: // biome-ignore lint/correctness/noUnusedVariables: used in template
[islands]  8: let count = $state(startAt)
[islands]                               ^
[islands]  9: </script>

* [ ] and also this error at runtime (although the component renders and functions):

 Failed to render Phoria island. {
   component: 'SvelteCounter',
   error: TypeError: Cannot read properties of null (reading 'function')
       at Module.push_element (/home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/dev.js:59:25)
        at eval (/home/meeg/projects/cmeeg/phoria/examples/framework-multiple/WebApp/ui/src/components/Counter/Counter.svelte:24:26)
       at Renderer.child (file:///home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/renderer.js:214:18)
       at Renderer.component (file:///home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/renderer.js:319:22)
        at Counter (/home/meeg/projects/cmeeg/phoria/examples/framework-multiple/WebApp/ui/src/components/Counter/Counter.svelte:13:13)
       at #open_render (file:///home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/renderer.js:789:4)
       at #render (file:///home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/renderer.js:638:42)
       at Object.get (file:///home/meeg/projects/cmeeg/phoria/node_modules/.pnpm/svelte@5.56.8/node_modules/svelte/src/internal/server/renderer.js:534:39)
       at i (file:///home/meeg/projects/cmeeg/phoria/packages/phoria-svelte/dist/server.js:10:4)
       at Object.render (file:///home/meeg/projects/cmeeg/phoria/packages/phoria-svelte/dist/server.js:19:57)
 }

* [x] Structured OTel logs now reach the Aspire dashboard, with console fallback when OTel logging is disabled.

* [ ] what is "pnpm biome check remains blocked by a pre-existing root biome.jsonc formatting error and existing warnings."?

* [ ] I need to test the various modes of the examples
  * [ ] Dev (Aspire)
  * [ ] Dev (non-Aspire)
  * [ ] Preview (Aspire)
  * [ ] Production (Docker)

### Startup

The example preview flow is now documented in `AGENTS.md`; keep verification
of the Dev, Preview, and Production modes in the checklist above.

  ➜ Local:    http://localhost:5173/
  ➜ Network:  use --host to expose

Using launch settings from ./WebApp/Properties/launchSettings.json...
Building...
/home/meeg/projects/cmeeg/phoria/packages/Phoria/Server/PhoriaServerProcess.cs(91,62): warning CA1873: Evaluation of this argument may be expensive and unnecessary if logging is disabled (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873)


### Exit

info: System.Net.Http.HttpClient.PhoriaServerHttpClient.LogicalHandler[101]
      End processing HTTP request after 2.5536ms - 200
^Cinfo: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...


phoria on  feature/upgrade via  v10.0.302 via  v24.16.0 took 2m45s
[ELIFECYCLE] Command failed.

^C^C
[1]+  Done                    "$_od_bin" --port 7456 --no-open > /dev/null 2>&1  (wd: ~/projects)
(wd now: ~/projects/cmeeg/phoria)

### Coding models

[According to Google](https://share.google/aimode/aCsNxXtB9xeUeS0EG)

The best free and low-cost coding models available on OpenCode Zen are ranked by performance, cost, and coding agent reliability.

* S-Tier (Completely Free / Zero Cost)
  * Big Pickle (big-pickle): A top-performing stealth model optimized for coding agents that handles tool usage and protocol searches exceptionally well.
  * MiMo-V2.5 Free (mimo-v2.5-free): Community favorite for strict instruction following and low error rates during execution.
  * DeepSeek V4 Flash Free (deepseek-v4-flash-free): Outstanding speed-to-performance ratio for routine sub-agent and editing tasks.
* A-Tier (Low-Cost Pay-As-You-Go / High Value)
  * DeepSeek V4 Flash / Pro (deepseek-v4-flash, deepseek-v4-pro): Highly efficient for splitting workflows—use Flash for execution and Pro for planning.
  * MiMo V2.5 Pro (mimo-v2.5-pro): A robust daily driver with strong reasoning capabilities before scaling up to expensive flagship models.
  * Qwen3.7 Plus (qwen3.7-plus): Excellent for exploration, context reading, and scouting tasks prior to main code generation.
  * GPT-5.6 Luna (gpt-5.6-luna): Reliable mid-tier performance with massive context window handling.
* B-Tier (Budget Fallbacks & Specialized)
  * MiniMax M3 / M2.5 (minimax-m3): High throughput and decent long-context support, though occasionally trails behind DeepSeek or MiMo in fine-grained logic.
  * Nemotron 3 Super Free (nemotron-3-super-free): Useful NVIDIA-hosted open-weight model with a large context window, best verified for structural correctness.
  * Kimi K2.6 / K2.7 Code (kimi-k2.7-code): Specialized code alternative, great for multi-agent coordination or swarms, though slightly more expensive per token than Flash models.

Also see [Kilo](https://kilo.ai/open-source-models)
