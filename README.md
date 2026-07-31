# Schedule parser NUST MISIS

Prototype package for the NUST MISIS Novotroitsk branch schedule parser.

Current version: `0.2.1`.

The repository contains two Windows 10 C# programs:

- `ScheduleDepartmentApp` - imports one or more Excel `.xlsx` schedules, parses groups, subgroups, lessons, teachers, rooms, remote lessons, Excel fill colors, exports JSON, and opens a local network HTTP server.
- `ScheduleViewerApp` - Windows viewer for students and teachers. It loads schedule JSON from a network URL or local file, filters by group or teacher, highlights current lessons, keeps Excel colors, and marks remote lessons in blue when they have no Excel color.
- Built-in web viewer - mobile/desktop browser page served by `ScheduleDepartmentApp` at `http://YOUR-PC-IP:5088/`.

The code is intentionally self-contained: it uses WinForms and a small OpenXML reader, without NuGet packages.

## Build

Run from the repository root:

```powershell
.\tools\build.ps1
```

If PowerShell script execution is disabled, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build.ps1
```

or use:

```cmd
tools\build.cmd
```

Output executables are created in `bin`:

- `bin\ScheduleDepartmentApp.exe`
- `bin\ScheduleViewerApp.exe`

## Run

```powershell
.\tools\run-department.ps1
.\tools\run-viewer.ps1
```

or:

```cmd
tools\run-department.cmd
tools\run-viewer.cmd
```

Typical flow:

1. Open `ScheduleDepartmentApp`.
2. Select one or more `.xlsx` schedule files.
3. Review parsed lessons and save JSON.
4. Start the HTTP server.
5. On a phone or another Windows device in the same Wi-Fi/LAN network, open the web URL shown by the department app, for example `http://192.168.1.20:5088/`.
6. For `ScheduleViewerApp`, use the JSON URL shown by the department app, for example `http://192.168.1.20:5088/schedule.json`.

For network mode the apps now try the real server address directly. If the schedule is unavailable, the error points to the server URL, firewall, or local network instead of relying on an external internet check.

If Windows Firewall asks for permission when the server starts, allow access on private networks.
