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
  : "${MEDILINK_DB_PASSWORD:?Set MEDILINK_DB_PASSWORD before running this script.}"
  : "${MEDILINK_JWT_SECRET:?Set MEDILINK_JWT_SECRET to a value with at least 32 characters.}"
  [ "${#MEDILINK_JWT_SECRET}" -ge 32 ] || {
    echo "MEDILINK_JWT_SECRET must contain at least 32 characters."
    exit 1
  }
}

verify_database_connection() {
  local host="${MEDILINK_DB_HOST:-localhost}"
  local port="${MEDILINK_DB_PORT:-3306}"
  local database="${MEDILINK_DB_NAME:-MediLink}"

  if ! MYSQL_PWD="$MEDILINK_DB_PASSWORD" mysql --protocol=TCP --host="$host" --port="$port" --user="$MEDILINK_DB_USERNAME" --execute "CREATE DATABASE IF NOT EXISTS \`$database\`;"; then
    echo "Unable to connect to MySQL as '$MEDILINK_DB_USERNAME' at $host:$port."
    echo "Check MEDILINK_DB_USERNAME and MEDILINK_DB_PASSWORD, then retry."
    exit 1
  fi
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
    export MEDILINK_DB_NAME="${MEDILINK_DB_NAME:-MediLink}"
    export MEDILINK_DB_URL="${MEDILINK_DB_URL:-jdbc:mysql://$MEDILINK_DB_HOST:$MEDILINK_DB_PORT/$MEDILINK_DB_NAME?useSSL=false&serverTimezone=UTC}"
    export ConnectionStrings__DefaultConnection="Server=$MEDILINK_DB_HOST;Port=$MEDILINK_DB_PORT;Database=$MEDILINK_DB_NAME;User ID=$MEDILINK_DB_USERNAME;Password=$MEDILINK_DB_PASSWORD;"
    export JwtSettings__Secret="$MEDILINK_JWT_SECRET"
    verify_database_connection
    start_service api "$ROOT_DIR" dotnet run --project src/MediLink.Api
    start_service web "$ROOT_DIR/src/MediLink.Web" npm run dev -- --host 127.0.0.1
    start_service store-portal "$ROOT_DIR/src/MediLink.Store.Java" mvn spring-boot:run
    printf '\nCustomer web:  http://localhost:5173\nAPI / Swagger: http://localhost:5140/swagger\nAPI health:    http://localhost:5140/health\nStore portal:  http://localhost:8081/login\n\n'
    ;;
  stop) stop_service api; stop_service web; stop_service store-portal ;;
  status)
    for name in api web store-portal; do
      pid_file="$RUNTIME_DIR/$name.pid"
      if [ -f "$pid_file" ] && kill -0 "$(cat "$pid_file")" 2>/dev/null; then echo "$name: running (PID $(cat "$pid_file"))"; else echo "$name: stopped"; fi
    done
    ;;
  *) echo "Usage: ./run.sh [start|stop|status]"; exit 1 ;;
esac
