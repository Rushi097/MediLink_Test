# MediLink continuation notes

## Current architecture

- React/Vite customer application: port `5173`.
- ASP.NET Core API: port `5140`; Swagger is `/swagger` and database health is `/health`.
- Spring Boot store portal: port `8081`.
- The .NET and Java services share one MySQL database, normally named `MediLink`.

## Launchers

- Windows: set `MEDILINK_DB_USERNAME`, `MEDILINK_DB_PASSWORD`, and
  `MEDILINK_JWT_SECRET`, then run `./run.cmd start` in PowerShell.
- Linux, macOS, and WSL: export the same variables, then run
  `bash run.sh start`.
- Stop or inspect Linux services with `bash run.sh stop` and
  `bash run.sh status`.

## WSL / Ubuntu guidance

- Keep the clone under the Linux filesystem (for example `~/projects/MediLink`),
  not `/mnt/d/...`. Windows native Node modules cannot be reused by Linux.
- After cloning under Linux, run `cd src/MediLink.Web && npm ci` once.
- On WSL, runtime logs and PID files are stored in
  `~/.local/state/medilink` to avoid Windows file-permission conflicts.
- WSL can use the same Windows MySQL server. Set `MEDILINK_DB_HOST=localhost`
  first; if localhost forwarding is unavailable, use the Windows gateway IP
  from `ip route show default | awk '{print $3}'`.
- Install and start the MySQL server in Ubuntu before starting MediLink.
- Use a dedicated MySQL account such as `medilink` instead of Ubuntu's
  socket-authenticated `root` account. Never commit passwords or JWT secrets.

## Health endpoint

`GET http://localhost:5140/health` confirms that the API can open a database
connection. It returns HTTP 200 with `Healthy` when MySQL is reachable, and
HTTP 503 if the database is unavailable. A wrong database password prevents the
API from starting, so `run.sh` now tests the MySQL login before it starts any
service.

## Last investigated issue

The WSL session tried to start MySQL without MySQL installed, used Windows
`node_modules` from `/mnt/d`, and could not write the Windows-created
`.medilink-run` files. The launcher and setup guide now cover these cases.
