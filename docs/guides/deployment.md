# Deployment

This guide will describe the steps required to deploy a [production build](./building-for-production.md) of a Phoria solution to [Azure Container Apps](#deploy-to-azure-container-apps) using Docker and the Azure Developer CLI (azd).

> [!TIP]
> If you cloned an example project to get started you should already have build scripts included in your project. You may also have a Dockerfile and/or scripts for deployment included depending on the example project. This guide is for those who want to setup deployment themselves, or for those who want to understand more about how Phoria is deployed.

> [!IMPORTANT]
> Currently the guide is focused on deploying to Azure Container Apps, but some of the principles should be applicable to other hosting providers (e.g. AWS) and scenarios (e.g. Aspire, Azure App Service) especially those that support Docker or hosting of dotnet apps in general.
>
> If you have questions or problems deploying Phoria to other hosting providers or would like to see this documentation expanded to support other platforms or scenarios please raise an issue, or raise a pull request to contribute to this documentation.

## Deploy to Azure Container Apps

[Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview) is a serverless platform for running containerised applications and services. It is powered by Kubernetes, but is a fully managed service that is designed to be simple to use and cost-effective.

[azd](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/overview) simplifies provisioning and deploying resources to Azure.

By following this guide you will:

* [Add a Dockerfile](#add-a-dockerfile) to create a container image for your Phoria app
* [Setup azd](#setup-azd) to provision a Azure Container Apps environment
* [Deploy the Docker container image](#deploy-with-azd) to Azure Container Apps using azd

The example project used in this guide builds upon the sample app created in the [Getting started](./getting-started.md) guide and assumes that you already have functioning [build scripts](./building-for-production.md) for your app. You may need to adjust some of the paths and commands in this guide to match your project structure.

### Prerequisites

There is some prerequisite software you will need to have installed before you go any further:

* [Docker Desktop](https://docs.docker.com/desktop/)
* [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd)

### Add a Dockerfile

First thing you will need to do is add a Dockerfile to describe how to create a container image for your Phoria app. The image will contain the build output of the Phoria Web App, Phoria Islands and the Phoria Server.

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

You will also need to add a `.dockerignore` file in your repository root to exclude files and directories that you don't want to include in the container image:

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
> If you have seen reference in other guides to the Phoria Server being a "sidecar" to the Phoria Web App and are wondering in that case why we are only building a single container image that contains both the Phoria Web App and the Phoria Server - the reason is that azd [does not currently support](https://github.com/Azure/azure-dev/issues/3239) sidecar containers.
>
> If you are not using azd however, you should be able to split up the above Dockerfile to build two container images, one for the dotnet web app, and one for the Phoria Server.

You can configure the Phoria Web App to start and monitor the Phoria Server `node` process by adding the following config to `WebApp/appsettings.Production.json`:

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

Now you can build and run the container image using Docker from the root of your repo:

```shell
# Build the container image
docker build -f ./WebApp/Dockerfile -t phoriaapp:latest .

# Run the container image
docker run --name phoriaapp -d -p 3001:8080 phoriaapp:latest
```

> [!NOTE]
> The tag `phoriaapp:latest` and name `phoriaapp` are just examples - you can use any tag or name that you like. Also please feel free to use a host port of your choosing rather than `3001` if you wish.

The Phoria app running in the Docker container will now be accessible at [http://localhost:3001](http://localhost:3001) to test everything is working as expected.

When you're done testing you can stop and remove the container image:

```shell
# Stop the container image
docker stop phoriaapp

# Remove the container image
docker rm phoriaapp
```

### Setup azd

Start by running the following command in the root of your repository:

```shell
azd init
```

Then you can proceed through the prompts to setup azd:

* You should be asked how you want to initialise your app - choose "Use code in the current directory"
  * This may not detect the correct services initially so pay attention before you proceed - you can add and remove services using the prompts provided - what you are aiming to do is to add just the Phoria dotnet Web App
  * For example, when running `azd init` on the "Getting started" example project a "Vite" service was detected automatically by azd in the root of the repo, but that had to be removed and the dotnet app under `./WebApp` had to be added manually instead
* `azd` should suggest hosting your app in Azure Container Apps - agree to proceed
* You should then be prompted to enter a new environment name - enter what you would like here

`azd` will then initialise your app by creating and modifying some files in your repository. The ones we want to draw your attention to are:

* The `infra` directory - this contains the Bicep files that describe the Azure resources that will be provisioned
* The `azure.yaml` file - this contains the deployment configuration for your app
* The `.gitignore` file - this has been modified to exclude certain files and directories that `azd` generates and uses, but do not need to be under source control

Any other files produced by the initialisation process are not relevant to this guide and you can decide if you want to keep them or not.

> [!TIP]
> Feel free to take a look through the `infra` directory if you are interested in how the Azure resources are described, but we won't be going into detail about that in this guide.
> 
> The idea is that `azd` gives you everything that you need to get started quickly and easily, but if you want to dig deeper and customise things further you can absolutely do that.
> 
> You can also use [Terraform instead of Bicep](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/use-terraform-for-azd) if you prefer.
>
> This guide also isn't aiming to be a guide to `azd` itself so please refer to the official documentation if you have more specific questions.

You will need to make some changes to the `azure.yaml` file for `azd` to use the Dockerfile that you created earlier. Here is an example of what the `azure.yaml` file should look like - the only part you should need to add is the `docker` section for the `web-app` service:

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Azure/azure-dev/main/schemas/v1.0/azure.yaml.json

name: getting-started
metadata:
  template: azd-init@1.11.1
services:
  web-app:
    project: WebApp
    host: containerapp
    language: dotnet
    docker:
      path: ./Dockerfile
      context: ../
```

> [!NOTE]
> The `path` should be the path to the Dockerfile that you created earlier, and the `context` should be the path to the root of your repository. Both of these paths are relative to the root of the service.

### Deploy with azd

If this is the first time you have used `azd` or you have not used it in a while you will need to sign in first:

```shell
azd auth login
```

Then you can use the following command to provision the Azure resources required and deploy your app to Azure Container Apps:

```shell
azd up
```

You will have to follow some prompts to select the Azure subscription and region that you want to provision your resources in, but then the process should be fully automated and you should see the output of the deployment in your terminal.

Once the deployment is complete you should be presented with a URL that you can use to access your Phoria app running in Azure Container Apps.

> [!TIP]
> If you make a change to your app you can deploy the changes by running `azd up` again - `azd` will detect if any changes have been made to the hosting environment and skip the provisioning step if it is not necessary.

If you don't want to keep the resources that were provisioned you can run the following command to tear them down:

```shell
azd down --purge
```

### Next steps

You may wish to [configure and use `azd` in a CI/CD pipeline](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/configure-devops-pipeline) to automate the deployment of your Phoria app to Azure Container Apps.
