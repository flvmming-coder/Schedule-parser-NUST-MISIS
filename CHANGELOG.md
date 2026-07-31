# Changelog

## 0.2.1

- Removed false blocking internet checks before server start and schedule loading.
- The department app now starts the LAN server directly and reports real server/port problems instead.
- The Windows viewer now tries the entered schedule URL first and shows server/URL errors instead of a generic "no internet" message.
- The viewer accepts both `http://IP:5088/` and `http://IP:5088/schedule.json`.

## 0.2

- Replaced the local-only `HttpListener` publication with a TCP HTTP server bound to all network interfaces.
- Added a built-in browser web viewer for phones and other devices at `/`.
- Kept JSON endpoints at `/schedule.json` and `/api/schedule` for the Windows viewer.
- Added clearer LAN links in the department app.
- Refreshed the visual style of both Windows programs.

## 0.1

- Initial C# WinForms prototype.
- Excel `.xlsx` parser for groups, subgroups, lessons, teachers, rooms, remote lessons, and fill colors.
- JSON export and Windows viewer prototype.
