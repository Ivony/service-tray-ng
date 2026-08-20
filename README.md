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
- Per-service port setting + auto-switch to the next free port when occupied (switch is temporary unless enabled; see `RememberChangedPort`)
- Start tray on login (registry `Run` key); per-service auto-start on launch
- Single config file with per-service sections
- Per-service daily logs; single-instance guard
- Localized UI: menu, status, balloon tips and dialogs follow the system UI language. Supported: en, zh, ja, ko, fr, de, es, it, pt, ru (falls back to English).

## Configuration

`%LOCALAPPDATA%\service-tray-ng-<edition>\config.json` — the config directory is
isolated per edition (`service-tray-ng-all`, `service-tray-ng-dsh`,
`service-tray-ng-opencode`), so each variant keeps its own settings.

```json
{
  "Services": {
    "opencode": {
      "ExecutablePath": "",
      "Hostname": "127.0.0.1",
      "Port": 4096,
      "AutoStartService": false,
      "AutoChangePort": true,
      "RememberChangedPort": false,
      "WorkingDirectory": ""
    },
    "dsh": {
      "ExecutablePath": "",
      "Hostname": "127.0.0.1",
      "Port": 3080,
      "AutoStartService": false,
      "AutoChangePort": true,
      "RememberChangedPort": false,
      "WorkingDirectory": ""
    }
  }
}
```

- `ExecutablePath`: absolute path to the binary (`.cmd`/`.bat` supported). Empty = resolve on PATH, falling back to `npx <package>` when the profile declares one.
- `Hostname`/`Port`: where the server binds. Defaults per profile.
- `AutoChangePort`: when the configured port is taken, pick the next free one.
- `RememberChangedPort`: persist an auto-switched port to the config file. Default `false` — the switch is temporary (used for this run only, UI and health checks follow it) and the configured port is used again on the next start. Manual port changes are always saved.
- `WorkingDirectory`: directory the server process runs in. Empty = the tray's own directory.
- `AutoStartService`: launch this service when the tray starts.

Note: `dsh web` requires Node.js and does not support `--host 0.0.0.0`.

## Build

Publish three editions from the same project — pass `-p:Edition=` to select which
service(s) the binary manages. Each edition gets its own exe name, single-instance
lock, registry `Run` value and config path, so all three can be installed and run
side by side.

```powershell
# All-in-one: OpenCode + Dsh (default, same as no -p:Edition)
dotnet publish -c Release -r win-x64 -p:Edition=all

# DeepSeek Harness only
dotnet publish -c Release -r win-x64 -p:Edition=dsh

# OpenCode only
dotnet publish -c Release -r win-x64 -p:Edition=opencode
```

Single-file exes at `bin/Release/net8.0-windows/win-x64/publish/`:
`service-tray-ng.exe`, `service-tray-ng-dsh.exe`, `service-tray-ng-opencode.exe`.

## Tests

```powershell
dotnet test
```

xUnit tests cover command resolution, port probing/switching, config load/fallback/persistence, profile defaults, and a real start/stop/restart lifecycle against a local node server.

## Adding a service

Add a `ServiceProfile` in `ServiceProfile.cs` (command names, `npx` package, args template, default port, logos) and drop the icons into `Assets/`.
