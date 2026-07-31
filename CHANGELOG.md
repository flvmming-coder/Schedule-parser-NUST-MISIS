# Changelog

## 0.4.1

- Fixed `ScheduleViewerApp` timeout when loading from the local server.
- Replaced UI-thread-blocking `WebClient.DownloadStringAsync` usage with a direct `HttpWebRequest` loader.
- Verified loading from a running local `ScheduleDepartmentApp` server on port `5088`.

## 0.4

- Added course metadata to imported lessons and JSON.
- Added week type metadata (`Четная`, `Нечетная`, `Не указана`) to imported lessons and JSON.
- Added course selection before import in the department app.
- Added current schedule cleanup: stops the server, clears the grid, and deletes the published JSON.
- Importing new Excel files while the server is running now restarts the server with the new schedule.
- Added course and week filters to the Windows viewer and built-in web viewer.
- Hardened viewer filtering so loaded schedules are displayed after course/week filtering.

## 0.3

- Fixed `ScheduleViewerApp` loading from local server addresses.
- The viewer now accepts `localhost`, `127.0.0.1`, bare host addresses without `http://`, root server URLs, `file://` URLs, and direct paths to local JSON files.
- The local server now also attempts to listen on IPv6 so `localhost` works when Windows resolves it to `::1`.
- The department app now shows `127.0.0.1` as the local URL to avoid localhost IPv6 ambiguity.

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
