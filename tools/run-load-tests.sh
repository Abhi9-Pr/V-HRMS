#!/usr/bin/env bash
# Runs the k6 scripts (k6/) against an already-running docker-compose stack — k6 isn't a
# docker-compose service itself (it runs a test and exits, not a long-running process), just a
# one-shot container attached to the same network. Requires `docker compose up -d` to have been
# run first.
#
# Usage:
#   ./tools/run-load-tests.sh seed                         # seed-employees.js, 5000 employees
#   ./tools/run-load-tests.sh seed 500                      # seed-employees.js, 500 employees
#   ./tools/run-load-tests.sh dashboard
#   ./tools/run-load-tests.sh payroll-dry-run
#   ./tools/run-load-tests.sh attendance-grid
#   ./tools/run-load-tests.sh all                            # seed (5000) then all three load tests

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
network="$(basename "$repo_root" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9')_default"
image="grafana/k6"

run_script() {
    local script="$1"
    shift
    echo "==> Running k6/${script}"
    docker run --rm --network "$network" -v "$repo_root/k6:/scripts" -e BASE_URL=http://api:8080 "$@" \
        "$image" run "/scripts/${script}"
}

case "${1:-}" in
    seed)
        run_script seed-employees.js -e "EMPLOYEE_COUNT=${2:-5000}"
        ;;
    dashboard)
        run_script dashboard.js
        ;;
    payroll-dry-run)
        run_script payroll-dry-run.js
        ;;
    attendance-grid)
        run_script attendance-grid.js
        ;;
    all)
        run_script seed-employees.js -e "EMPLOYEE_COUNT=${2:-5000}"
        run_script dashboard.js
        run_script payroll-dry-run.js
        run_script attendance-grid.js
        ;;
    *)
        echo "Usage: $0 {seed [count]|dashboard|payroll-dry-run|attendance-grid|all [seed-count]}" >&2
        exit 1
        ;;
esac
