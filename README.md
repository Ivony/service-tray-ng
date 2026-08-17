# service-tray-ng

Windows system tray app that manages multiple CLI service servers from a single process — one tray icon per service.

Combines [opencode-service-tray](https://github.com/Ivony/opencode-service-tray) and [dsh-service-tray](https://github.com/Ivony/dsh-service-tray) into one project. Add a new managed service by declaring one `ServiceProfile`.

## Managed services

| Icon | Command | Default port |
|---|---|---|
| OpenCode | `opencode serve --hostname {host} --port {port}` | 4096 |
| Dsh | `npx @deepseek-ai/dsh web --host {host} --port {port}` (or `dsh web`) | 3080 |

Each service gets its own tray icon, context menu, status dot, port, config, and logs — start/stop/restart are fully independent.

## Features

- One process, one tray icon per service (status-aware, adapts to dark/light mode)
- Per-service start / stop / restart with 2s TCP health polling
- Per-service port setting + auto-switch to the next free port when occupied
- Start tray on login (registry `Run` key); per-service auto-start on launch
- Single config file with per-service sections
- Daily per-service logs; single-instance guard

## Configuration

`%LOCALAPPDATA%\service-tray-ng\config.json`

```json
{
  "Services": {
    "opencode": {
      "ExecutablePath": "",
      "Hostname": "127.0.0.1",
      "Port": 4096,
      "AutoStartService": false,
      "AutoChangePort": true,
      "WorkingDirectory": ""
    },
    "dsh": {
      "ExecutablePath": "",
      "Hostname": "127.0.0.1",
      "Port": 3080,
      "AutoStartService": false,
      "AutoChangePort": true,
      "WorkingDirectory": ""
    }
  }
}
```

- `ExecutablePath`: absolute path to the binary (`.cmd`/`.bat` supported). Empty = resolve on PATH, falling back to `npx <package>` when the profile declares one.
- `Hostname`/`Port`: where the server binds. Defaults per profile.
- `AutoChangePort`: when the configured port is taken, pick the next free one and remember it.
- `WorkingDirectory`: directory the server process runs in. Empty = the tray's own directory.
- `AutoStartService`: launch this service when the tray starts.

Note: `dsh web` requires Node.js and does not support `--host 0.0.0.0`.

## Build

```powershell
dotnet publish -c Release -r win-x64
```

Single-file exe at `bin/Release/net8.0-windows/win-x64/publish/`.

## Adding a service

Add a `ServiceProfile` in `ServiceProfile.cs` (command names, `npx` package, args template, default port, logos) and drop the icons into `Assets/`.
