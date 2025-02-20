# Deployment

This guide will describe the steps required to deploy a [production build](./building-for-production.md) of a Phoria solution to [Azure Container Apps](#deploy-to-azure-container-apps) using Docker and the Azure Developer CLI (azd).

> [!TIP]
> If you cloned an example project to get started you should already have build scripts included in your project. You may also have scripts for deployment included depending on the example project. This guide is for those who want to setup deployment themselves, or for those who want to understand more about how Phoria is deployed.

> [!IMPORTANT]
> Currently the guide is focused on deploying to Azure Container Apps, but some of the principles should be applicable to other hosting providers (e.g. AWS) and scenarios (e.g. Aspire, Azure App Service) especially those that support Docker or hosting of dotnet apps in general.
>
> If you have questions or problems deploying Phoria to other hosting providers or would like to see this documentation expanded to support other platforms or scenarios please raise an issue, or raise a pull request to contribute to this documentation.

## Deploy to Azure Container Apps

[Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview) is a serverless platform for running containerised applications and services. It is powered by Kubernetes, but is a fully managed service that is designed to be simple to use and cost-effective.

[azd](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/overview) simplifies provisioning and deploying resources to Azure.

In this guide we are going to:

* [Add a Dockerfile](#add-a-dockerfile) to create a container image for our Phoria app
* [Setup azd](#setup-azd) to provision a Azure Container Apps environment
* [Deploy the Docker container image](#deploy-with-azd) to Azure Container Apps using azd

The example used builds upon the sample app created in the [Getting started](./getting-started.md) guide and assumes that you already have functioning [build scripts](./building-for-production.md) for your app.

### Prerequisites

There is some prerequisite software you will need to have installed before we go any further:

* [Docker Desktop](https://docs.docker.com/desktop/)
* [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)

### Add a Dockerfile

First thing we will do is add a Dockerfile to describe how to create a container image for our Phoria app. The image will contain the build output of the Phoria Web App, Islands and the Phoria Server.

```dockerfile
# UI build stage
FROM node:22-slim AS uibuild
ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
WORKDIR /src

## Copy source code
COPY . .

## Install deps
RUN corepack enable
RUN --mount=type=cache,id=pnpm,target=/pnpm/store pnpm install --frozen-lockfile

## Set env for build
ENV NODE_ENV=production
ENV DOTNET_ENVIRONMENT=Production

## Build Phoria Islands and Server
RUN pnpm run build:islands
RUN pnpm run build:server

## Create deployment package
RUN mkdir -p /app/WebApp/ui \
  && cp -r /src/WebApp/ui/dist /app/WebApp/ui/dist \
  && cp -r /src/node_modules /app/node_modules \
  && cp /src/package.json /app/package.json

# Dotnet build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dotnetbuild
WORKDIR /src

## Copy source code
COPY . .

## Restore all projects
RUN dotnet restore

## Create deployment package
RUN dotnet publish ./WebApp/WebApp.csproj -c Release --no-restore -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV NODE_ENV=production
ENV DOTNET_ENVIRONMENT=Production
WORKDIR /app

## Copy dotnetbuild assets
COPY --from=dotnetbuild /app .

## Rename the WebApp binary as otherwise it will conflict with the WebApp directory from the uibuild
RUN mv /app/WebApp /app/WebAppCmd

## Copy uibuild assets
COPY --from=uibuild /app .

# Install node for Phoria Server
ENV NODE_VERSION=22.11.0
RUN apt-get -y update \
	&& apt-get install -y curl \
	&& curl -fsSL https://deb.nodesource.com/setup_${NODE_VERSION} -o nodesource_setup.sh | bash \
	&& apt-get install -y --no-install-recommends nodejs \
	&& apt-get clean

# Run the app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["./WebAppCmd"]
```

We will also add a `.dockerignore` file in our repository root to exclude files and directories that we don't want to include in the container image:

```plaintext
# directories
**/bin/
**/dist/
!**/wwwroot/lib/**/dist/
**/node_modules/
**/obj/

# files
Dockerfile*
**/*.DS_Store
**/*.env
**/*.md
**/*.tmp
**/*.tsbuildinfo
```

> [!NOTE]
> If you have seen reference in other guides to the Phoria Server being a "sidecar" to the dotnet web app and are wondering in that case why we are only building a single container image that contains both the dotnet web app and the Phoria Server - the reason is that azd [does not currently support](https://github.com/Azure/azure-dev/issues/3239) sidecar containers.
>
> If you are not using azd however, you should be able to split up the above Dockerfile to build two container images, one for the dotnet web app, and one for the Phoria Server.

We can configure the Phoria web app to start and monitor the Phoria Server process by adding the following config to `WebApp/appsettings.Production.json`:

```json
{
	"phoria": {
		"server": {
			"process": {
				"command": "node",
				"arguments": ["WebApp/ui/dist/server/server.js"]
			}
		}
	}
}
```

Now we can build and run the container image using Docker from the root of the repo:

```shell
# Build the container image
docker build -f ./WebApp/Dockerfile -t phoriaapp:latest .

# Run the container image
docker run --name phoriaapp -d -p 3001:8080 phoriaapp:latest
```

The Phoria app running in the Docker container will now be accessible at [http://localhost:3001](http://localhost:3001) to test everything is working as expected.

When you're done testing you can stop and remove the container image:

```shell
# Stop the container image
docker stop phoriaapp

# Remove the container image
docker rm phoriaapp
```

### Setup azd

TODO

### Deploy with azd

TODO
