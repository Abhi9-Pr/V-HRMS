#!/usr/bin/env bash
# Manage the Vespera dev database Docker container.
#
# Thin wrapper around the docker CLI for the same container Vespera.Api's Docker provisioning
# strategy manages (default name: vespera-db-dev, label com.vespera.managed=true). Defaults
# match Vespera.Api's appsettings.Development.json — override via env vars if you've changed
# those.
#
# Usage: tools/vespera-db.sh {up|down|reset|status|logs}
set -euo pipefail

CONTAINER_NAME="${VESPERA_DB_CONTAINER_NAME:-vespera-db-dev}"
VOLUME_NAME="${VESPERA_DB_VOLUME_NAME:-vespera-db-dev-data}"
IMAGE="${VESPERA_DB_IMAGE:-postgres:16-alpine}"
PORT="${VESPERA_DB_PORT:-55432}"
USERNAME="${VESPERA_DB_USERNAME:-postgres}"
PASSWORD="${VESPERA_DB_PASSWORD:-postgres}"

usage() {
    echo "Usage: $0 {up|down|reset|status|logs}"
    exit 1
}

cmd_up() {
    if docker ps -a --filter "name=^/${CONTAINER_NAME}\$" --format '{{.ID}}' | grep -q .; then
        echo "Starting existing container '${CONTAINER_NAME}'..."
        docker start "${CONTAINER_NAME}" > /dev/null
    else
        echo "Creating container '${CONTAINER_NAME}' on port ${PORT}..."
        docker run -d \
            --name "${CONTAINER_NAME}" \
            --label "com.vespera.managed=true" \
            -e "POSTGRES_USER=${USERNAME}" \
            -e "POSTGRES_PASSWORD=${PASSWORD}" \
            -p "${PORT}:5432" \
            -v "${VOLUME_NAME}:/var/lib/postgresql/data" \
            "${IMAGE}" > /dev/null
    fi
    echo "Vespera dev database is up on port ${PORT}."
}

cmd_down() {
    echo "Stopping container '${CONTAINER_NAME}'..."
    docker stop "${CONTAINER_NAME}" > /dev/null 2>&1 || true
}

cmd_reset() {
    echo "Removing container '${CONTAINER_NAME}' and volume '${VOLUME_NAME}'..."
    docker rm -f "${CONTAINER_NAME}" > /dev/null 2>&1 || true
    docker volume rm "${VOLUME_NAME}" > /dev/null 2>&1 || true
    cmd_up
}

cmd_status() {
    docker ps -a --filter "name=^/${CONTAINER_NAME}\$" --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
}

cmd_logs() {
    docker logs -f "${CONTAINER_NAME}"
}

case "${1:-}" in
    up) cmd_up ;;
    down) cmd_down ;;
    reset) cmd_reset ;;
    status) cmd_status ;;
    logs) cmd_logs ;;
    *) usage ;;
esac
