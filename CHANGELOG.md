# Changelog

## 0.5.4

- Fixed stale global publication: schedule JSON now carries a heartbeat timestamp.
- The web schedule page now hides stale global data and shows a schedule unavailable state instead.
- The department server can start without loaded Excel files.
- Local and global web pages now receive a valid unavailable schedule package when no files are loaded.
- Global publication now prepares the same server package used by local access.
- Added a separate administrator launcher.

## 0.5.3

- Added automatic GitHub Pages synchronization while the department server is running.
- The department app now republishes the global schedule immediately after server start and then every 5 minutes.
- Added an `Автообновлять сайт` switch for the global publication loop.
- The browser schedule page now checks `schedule.json` every 5 minutes and shows a no-connection state when the ping fails.
- Network errors on the protected schedule page no longer force a password reset unless the password itself is invalid.

## 0.5.2

- Added a password visibility eye button on the browser welcome page.
- Added a custom browser password field for protected global publication; the default remains `Student2026`.
- Split the web version into a welcome/password page and a separate schedule page.
- Added a loaded Excel files manager in the department app with add, replace, delete, and apply actions.
- The global publisher now uploads both `index.html` and `schedule.html`.
- The viewer URL normalizer accepts `schedule.html` links and resolves them to `schedule.json`.

## 0.5.1

- Added protected global browser access for GitHub Pages publication.
- The department app now has a `Защищенный канал` switch for global publication.
- Protected publication encrypts `schedule.json` and the browser page asks for a password before showing the schedule.
- Open publication still produces ordinary JSON for unrestricted access.
- Windows apps keep working without a password prompt; protected JSON is opened by the shared core logic.

## 0.5

- Added global schedule publication through GitHub Pages.
- The department app can publish the current `schedule.json` and web viewer to the `gh-pages` branch.
- The global publication can use a pasted GitHub token, `GITHUB_TOKEN`, or an existing Git Credential Manager login.
- The web viewer now loads `schedule.json` relative to its current page, so it works both locally and under GitHub Pages subpaths.
- The Windows viewer now normalizes GitHub Pages page URLs to their `schedule.json` endpoint.

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
