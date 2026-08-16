FROM node:24.18-bookworm-slim AS ui-build

WORKDIR /src
RUN corepack enable && corepack prepare pnpm@11.17.0 --activate

COPY package.json pnpm-workspace.yaml pnpm-lock.yaml ./
COPY apps/WebApp/package.json apps/WebApp/package.json
COPY packages/ui/package.json packages/ui/package.json
RUN pnpm install --frozen-lockfile

COPY apps/WebApp/ apps/WebApp/
COPY packages/ui/ packages/ui/
RUN pnpm --filter @phoriaexamples/ui build && pnpm --filter with-workspace build:islands && pnpm prune --prod

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY apps/WebApp/ apps/WebApp/
RUN dotnet publish apps/WebApp/WebApp.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    NODE_ENV=production

COPY --from=dotnet-build /app/publish ./
COPY --from=ui-build /src/apps/WebApp/ui/dist ./ui/dist
COPY --from=ui-build /src/node_modules ./node_modules
COPY --from=ui-build /src/apps/WebApp/package.json ./package.json
COPY --from=ui-build /usr/local/bin/node /usr/local/bin/node

EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApp.dll"]
