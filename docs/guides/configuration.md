# Configuration

> [!NOTE]
> This guide is incomplete. It will cover every `PhoriaOptions` setting and its default value. For now, [Getting started](./getting-started.md) covers the options the first-run path uses, and each default is set at its declaration site in `packages/Phoria/PhoriaOptions.cs`.

## Defaults

Any configuration option that is not set explicitly will fallback to a default value except for `entry` and `ssrEntry` because there are no sensible defaults for these options.

The `https` option will default to `false`.

## Related

- [Getting started](./getting-started.md) — the first-run path, with each configuration step in the order it needs them.
- [Phoria Web App](./phoria-web-app.md) — what the app does with the configuration it is given.
- [Phoria Server](./phoria-server.md) — the server that the `server` section configures, and the health behaviour behind it.
