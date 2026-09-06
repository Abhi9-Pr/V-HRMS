# syntax=docker/dockerfile:1
#
# Build context is the REPO ROOT, not Vespera.Client/ — this workspace uses npm workspaces (see
# the root package.json's "workspaces" list), so there's a single package-lock.json at the repo
# root, not one per workspace. `npm ci` needs every listed workspace's package.json present to
# resolve against that lockfile, even though only Vespera.Client is actually built here.
#
# Build: `docker build -f Vespera.Client/Dockerfile .` from the repo root, or via docker-compose.yml.

FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
COPY Vespera.Client/package.json Vespera.Client/package.json
COPY Vespera.Mobile/package.json Vespera.Mobile/package.json
RUN npm ci

COPY Vespera.Client/ Vespera.Client/
WORKDIR /app/Vespera.Client

# vespera-shared must be built before the app: tsconfig.json path-maps the "vespera-shared" import
# specifier to its built dist/, not its source — see docs/CONTRIBUTING-frontend.md.
RUN npx ng build vespera-shared --configuration production
RUN npx ng build vespera-client --configuration production

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/Vespera.Client/dist/vespera-client/browser /usr/share/nginx/html
COPY Vespera.Client/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD wget -qO- http://localhost:80/ || exit 1
