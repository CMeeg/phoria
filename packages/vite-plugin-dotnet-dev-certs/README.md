# vite-plugin-dotnet-dev-certs

A Vite plugin to configure usage of [dotnet dev-certs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs) with a Vite development server.

## Linux certificate trust

The [ASP.NET Core developer certificate](https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs) is only trusted by OpenSSL when `SSL_CERT_DIR` includes `~/.aspnet/dev-certs/trust`. On Linux this directory is not part of the default search path, so OpenSSL-based clients (e.g. `curl` and Node's HTTPS) will reject connections to the Vite dev server's HTTPS endpoint, and `aspire run` reports a "Developer certificates may not be fully trusted" warning on every start.

To trust the certificate, add `export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"` to your shell profile and run `dotnet dev-certs https --trust` (or `aspire certs trust`) once.
