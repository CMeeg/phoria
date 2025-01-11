# ui build stage
FROM node:22-slim AS uibuild
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
WORKDIR /src

# Copy source code
COPY . .

# Install deps

RUN corepack enable
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile

# Build the app
ENV NODE_ENV=production
ENV DOTNET_ENVIRONMENT=Production
RUN pnpm lerna run build \
	&& pnpm --filter=with-workspace deploy --prod /app

# dotnet build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS appbuild
WORKDIR /src

# Copy source code
COPY . .

# Restore all projects
RUN dotnet restore ./e2e/with-workspace/WebApp/WebApp.csproj

# Publish web app
RUN dotnet publish ./e2e/with-workspace/WebApp/WebApp.csproj -c Release --no-restore -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV NODE_ENV=production
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app

# Copy built assets

COPY --from=appbuild /app .
COPY --from=uibuild /app/ui/dist ./ui/dist
COPY --from=uibuild /app/node_modules ./node_modules

# Install node

ENV NODE_VERSION=22.11.0
RUN apt-get -y update \
	&& apt-get install -y curl \
	&& curl -fsSL https://deb.nodesource.com/setup_${NODE_VERSION} -o nodesource_setup.sh | bash \
	&& apt-get install -y --no-install-recommends nodejs \
	&& apt-get clean

# Run the app

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["./WebApp"]
