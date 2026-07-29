#!/usr/bin/env bash
# MediLink local development launcher (Git Bash, WSL, Linux, or macOS).
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME_DIR="$ROOT_DIR/.medilink-run"
COMMAND="${1:-start}"

for command in dotnet node npm mvn; do
  command -v "$command" >/dev/null 2>&1 || { echo "Missing required command: $command"; exit 1; }
done

require_environment() {
  : "${MEDILINK_DB_PASSWORD:?Set MEDILINK_DB_PASSWORD before running this script.}"
  : "${MEDILINK_JWT_SECRET:?Set MEDILINK_JWT_SECRET to a value with at least 32 characters.}"
  [ "${#MEDILINK_JWT_SECRET}" -ge 32 ] || { echo "Hyperlocal online medicine ordering platform"; exit 1; }
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
    require_environment; mkdir -p "$RUNTIME_DIR"
    export MEDILINK_DB_URL="${MEDILINK_DB_URL:-jdbc:mysql://localhost:3306/MediLink?useSSL=false&serverTimezone=UTC}"
    export MEDILINK_DB_USERNAME="${MEDILINK_DB_USERNAME:-root}"
    export ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=MediLink;User ID=$MEDILINK_DB_USERNAME;Password=$MEDILINK_DB_PASSWORD;"
    export JwtSettings__Secret="$MEDILINK_JWT_SECRET"
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
