# Phoria Web App

A Phoria Web App is just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured.

> [!TIP]
> Phoria can be configured programmatically via `AddPhoria`, but it's recommended to use `appsettings` because then the `phoria` Vite plugin and the Phoria Server can read that same configuration from the `appsettings.*.json` files rather than having to duplicate it in each place.

## Supervising the Phoria Server

Phoria's middleware sits ahead of your endpoints. A request that has no matched endpoint, has a path, and is a `GET` is proxied to the Phoria Server once that server reports healthy — HMR requests through a WebSocket proxy, everything else over HTTP.

The app polls the server's `/hc` health endpoint in the background and keeps its status current, so the middleware knows whether the server is healthy before it decides what to do with a request. The app can also start the server process itself: configuring `Phoria:Server:Process` makes it spawn the `node` process directly, and restart it when it exits while unhealthy, up to a configurable limit.

The first health check is a gate rather than a poll. Until it succeeds the server's status is `Unknown` rather than healthy, so nothing is proxied to it. `StartupTimeout` bounds that wait and defaults to `0`, which waits indefinitely; a positive value that expires fails the wait, and the app stops rather than carrying on against a server that never came up.

## When the server is unavailable

`UnavailableBehavior` decides what happens while the server is not healthy, and it defaults to `Degrade`:

- `Degrade` passes the request on down the .NET pipeline, so the app keeps responding, just without the server's output. An Island whose component cannot be created is dropped from the page instead of failing it.
- `Fail` returns `503 Service Unavailable`, and an Island that cannot be created fails the page rather than being dropped silently. This is what you want when something above the app should restart the container; [Deployment](./deployment.md) covers that setup.

## Related

- [Phoria Server](./phoria-server.md) — the sidecar this app proxies to and, optionally, starts itself.
- [Deployment](./deployment.md) — running both halves as containers, with health reporting and a fail-fast startup.
- [Configuration](./configuration.md) — the options this app reads from `appsettings.*.json`, and the defaults it applies.
- [Phoria Islands](./phoria-islands.md) — the components this app hands to Phoria through its Tag Helpers.
