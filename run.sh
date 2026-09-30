#!/usr/bin/env bash
# MediLink local development launcher (Git Bash, WSL, Linux, or macOS).
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -n "${WSL_DISTRO_NAME:-}" ]; then
  RUNTIME_DIR="${MEDILINK_RUNTIME_DIR:-$HOME/.local/state/medilink}"
else
  RUNTIME_DIR="${MEDILINK_RUNTIME_DIR:-$ROOT_DIR/.medilink-run}"
fi
COMMAND="${1:-start}"

require_commands() {
  for command in dotnet node npm mvn mysql; do
    command -v "$command" >/dev/null 2>&1 || {
      echo "Missing required command: $command"
      echo "Install the Ubuntu prerequisites listed in ALL_LINKS_AND_PROJECT_GUIDE.md, then retry."
      exit 1
    }
  done
}

require_environment() {
  : "${MEDILINK_DB_USERNAME:=root}"
  : "${MEDILINK_DB_PASSWORD:=root}"
  : "${MEDILINK_JWT_SECRET:=medilink-development-secret-must-be-32-characters}"
  [ "${#MEDILINK_JWT_SECRET}" -ge 32 ] || {
    echo "MEDILINK_JWT_SECRET must contain at least 32 characters."
    exit 1
  }
}

verify_database_connection() {
  local host="${MEDILINK_DB_HOST:-localhost}"
  local port="${MEDILINK_DB_PORT:-3306}"
  local databases=("${MEDILINK_AUTH_DB_NAME:-MediLinkAuth}" "${MEDILINK_INVENTORY_DB_NAME:-MediLinkInventory}" "${MEDILINK_ORDER_DB_NAME:-MediLinkOrder}")

  for database in "${databases[@]}"; do
    if ! MYSQL_PWD="$MEDILINK_DB_PASSWORD" mysql --protocol=TCP --host="$host" --port="$port" --user="$MEDILINK_DB_USERNAME" --execute "CREATE DATABASE IF NOT EXISTS \`$database\`;"; then
      echo "Unable to connect to MySQL as '$MEDILINK_DB_USERNAME' at $host:$port."
      echo "Check MEDILINK_DB_USERNAME and MEDILINK_DB_PASSWORD, then retry."
      exit 1
    fi
  done
}

stop_service() {
  local name="$1" pid_file="$RUNTIME_DIR/$1.pid" 
  if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then kill "$(cat "$pid_file")"; echo "Stopped $name."; fi
  rm -f "$pid_file"
}

start_service() {
  local name="$1" directory="$2"; shift 2
  local pid_file="$RUNTIME_DIR/$name.pid" log_file="$RUNTIME_DIR/$name.log"
  if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then echo "$name is already running (PID $(cat "$pid_file"))."; return; fi
  (cd "$directory" && "$@") >"$log_file" 2>&1 &
  echo $! >"$pid_file"
  echo "Started $name (log: $log_file)."
}

case "$COMMAND" in
  start)
    require_commands
    require_environment
    mkdir -p "$RUNTIME_DIR"
    export MEDILINK_DB_HOST="${MEDILINK_DB_HOST:-localhost}"
    export MEDILINK_DB_PORT="${MEDILINK_DB_PORT:-3306}"
    export MEDILINK_AUTH_DB_NAME="${MEDILINK_AUTH_DB_NAME:-MediLinkAuth}"
    export MEDILINK_INVENTORY_DB_NAME="${MEDILINK_INVENTORY_DB_NAME:-MediLinkInventory}"
    export MEDILINK_ORDER_DB_NAME="${MEDILINK_ORDER_DB_NAME:-MediLinkOrder}"
    export MEDILINK_AUTO_CREATE_DATABASE="true"
    export ConnectionStrings__AuthConnection="Server=$MEDILINK_DB_HOST;Port=$MEDILINK_DB_PORT;Database=$MEDILINK_AUTH_DB_NAME;User ID=$MEDILINK_DB_USERNAME;Password=$MEDILINK_DB_PASSWORD;"
    export ConnectionStrings__InventoryConnection="Server=$MEDILINK_DB_HOST;Port=$MEDILINK_DB_PORT;Database=$MEDILINK_INVENTORY_DB_NAME;User ID=$MEDILINK_DB_USERNAME;Password=$MEDILINK_DB_PASSWORD;"
    export ConnectionStrings__OrderConnection="Server=$MEDILINK_DB_HOST;Port=$MEDILINK_DB_PORT;Database=$MEDILINK_ORDER_DB_NAME;User ID=$MEDILINK_DB_USERNAME;Password=$MEDILINK_DB_PASSWORD;"
    export JwtSettings__Secret="$MEDILINK_JWT_SECRET"
    verify_database_connection
    start_service api "$ROOT_DIR" dotnet run --project src/MediLink.Api
    start_service auth "$ROOT_DIR" dotnet run --project src/MediLink.Auth
    start_service inventory "$ROOT_DIR" dotnet run --project src/MediLink.Inventory
    start_service order "$ROOT_DIR" dotnet run --project src/MediLink.Order
    start_service web "$ROOT_DIR/src/MediLink.Web" npm run dev -- --host 127.0.0.1
    start_service store-portal "$ROOT_DIR/src/MediLink.Store.Java" mvn spring-boot:run
    printf '\nCustomer web:   http://localhost:5173\nCustomer login: http://localhost:5173/login\nAdmin login:    http://localhost:5173/admin-login\nAPI / Swagger:  http://localhost:5140/swagger\nAPI health:     http://localhost:5140/health\nAuth health:    http://localhost:5101/health\nInventory health: http://localhost:5201/health\nOrder health:   http://localhost:5301/health\nStore portal:   http://localhost:8081/login\n\n'
    ;;
  stop) stop_service api; stop_service auth; stop_service inventory; stop_service order; stop_service web; stop_service store-portal ;;
  status)
    for name in api auth inventory order web store-portal; do
      pid_file="$RUNTIME_DIR/$name.pid"
      if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then echo "$name: running (PID $(cat "$pid_file"))"; else echo "$name: stopped"; fi
    done
    ;;
  *) echo "Usage: ./run.sh [start|stop|status]"; exit 1 ;;
esac
