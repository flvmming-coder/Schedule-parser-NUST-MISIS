# Schedule parser NUST MISIS

Prototype package for the NUST MISIS Novotroitsk branch schedule parser.

The repository contains two Windows 10 C# programs:

- `ScheduleDepartmentApp` - imports one or more Excel `.xlsx` schedules, parses groups, subgroups, lessons, teachers, rooms, remote lessons, Excel fill colors, exports JSON, and can serve the current schedule over HTTP.
- `ScheduleViewerApp` - prototype viewer for students and teachers. It loads schedule JSON from a network URL or local file, filters by group or teacher, highlights current lessons, keeps Excel colors, and marks remote lessons in blue when they have no Excel color.

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
5. Open `ScheduleViewerApp` and load `http://localhost:5088/schedule.json` or the LAN URL shown by the department app.

For network loading the viewer checks internet/network availability and shows an error when the connection or schedule server is unavailable.
