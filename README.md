# Schedule parser NUST MISIS

Prototype package for the NUST MISIS Novotroitsk branch schedule parser.

Current version: `0.5.1`.

The repository contains two Windows 10 C# programs:

- `ScheduleDepartmentApp` - imports one or more Excel `.xlsx` schedules, parses groups, subgroups, lessons, teachers, rooms, remote lessons, Excel fill colors, exports JSON, and opens a local network HTTP server.
- `ScheduleViewerApp` - Windows viewer for students and teachers. It loads schedule JSON from a network URL or local file, filters by group or teacher, highlights current lessons, keeps Excel colors, and marks remote lessons in blue when they have no Excel color.
- Built-in web viewer - mobile/desktop browser page served by `ScheduleDepartmentApp` at `http://YOUR-PC-IP:5088/`.
- Multi-course and week-type storage - one schedule JSON can contain several courses and both even/odd weeks.
- Global GitHub Pages publication - the department app can publish the current schedule to a public HTTPS page available worldwide, independent of Wi-Fi/LAN.
- Protected browser access - the global web viewer can require a password before showing the schedule. The Windows apps do not show a password prompt.

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
3. Choose the courses you are loading.
4. Review parsed lessons and save JSON.
5. Start the HTTP server.
6. When the schedule changes, use `Очистить` or import new files while the server is running; the server restarts with the new publication.
7. On a phone or another Windows device in the same Wi-Fi/LAN network, open the web URL shown by the department app, for example `http://192.168.1.20:5088/`.
8. For `ScheduleViewerApp`, use the JSON URL shown by the department app, for example `http://192.168.1.20:5088/schedule.json`.
9. For worldwide access, choose `Защищенный канал` or open access, click `Опубликовать в интернет` in `ScheduleDepartmentApp`, and use the GitHub Pages URL.

For network mode the apps now try the real server address directly. The viewer accepts `http://127.0.0.1:5088/`, `http://localhost:5088/`, bare host addresses such as `localhost:5088`, GitHub Pages URLs such as `https://flvmming-coder.github.io/Schedule-parser-NUST-MISIS/`, `file://` URLs, and ordinary paths to local JSON files.

The LAN server is still useful inside one local network. For access from any city, provider, phone network, Wi-Fi, or Windows device, use the global GitHub Pages URL below. In protected mode the browser page starts with a password screen and `schedule.json` is published as an encrypted package; in open mode it is published as ordinary JSON.

Global URL:

```text
https://flvmming-coder.github.io/Schedule-parser-NUST-MISIS/
```

If Windows Firewall asks for permission when the server starts, allow access on private networks.
