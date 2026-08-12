FROM node:24.18-bookworm-slim AS ui-build

WORKDIR /src/WebApp

RUN corepack enable && corepack prepare pnpm@11.17.0 --activate

COPY WebApp/package.json WebApp/pnpm-lock.yaml WebApp/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY WebApp/ ./
RUN pnpm build:islands && pnpm prune --prod

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build

WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY WebApp/ WebApp/
RUN dotnet publish WebApp/WebApp.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    NODE_ENV=production

COPY --from=dotnet-build /app/publish ./
COPY --from=ui-build /src/WebApp/ui/dist ./ui/dist
COPY --from=ui-build /src/WebApp/node_modules ./node_modules
COPY --from=ui-build /src/WebApp/package.json ./package.json
COPY --from=ui-build /usr/local/bin/node /usr/local/bin/node

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApp.dll"]
