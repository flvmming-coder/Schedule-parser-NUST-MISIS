using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ScheduleParser.Core
{
    public sealed class XlsxScheduleParser
    {
        private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private static readonly Regex DateRegex = new Regex(@"(?<day>[А-Яа-яЁё]+).*?(?<date>\d{1,2}\.\d{1,2}\.\d{4})", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new Regex(@"(?<h1>\d{1,2})[\.:](?<m1>\d{2})\s*[-–—]\s*(?<h2>\d{1,2})[\.:](?<m2>\d{2})", RegexOptions.Compiled);
        private static readonly Regex TeacherRegex = new Regex(@"(?:/)?\s*(?<teacher>[А-ЯЁ][а-яё-]+(?:\s+[А-ЯЁ]\.?\s*[А-ЯЁ]\.?)?)\s*$", RegexOptions.Compiled);
        private static readonly Regex GroupRegex = new Regex(@"[А-ЯЁA-Z]{2,}[-–]\d{2}", RegexOptions.Compiled);

        public ScheduleDocument ParseFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Не указан путь к Excel-файлу.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Excel-файл не найден.", path);
            }

            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                WorkbookData workbook = ReadWorkbook(archive);
                ScheduleDocument document = new ScheduleDocument();
                document.ParsedAt = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
                document.SourceFiles.Add(Path.GetFileName(path));

                foreach (WorksheetData sheet in workbook.Worksheets)
                {
                    ParseSheet(sheet, path, document);
                    ParseDigitalTools(sheet, document);
                }

                RebuildIndexes(document);
                return document;
            }
        }

        public ScheduleDocument ParseFiles(IEnumerable<string> paths)
        {
            ScheduleDocument combined = new ScheduleDocument();
            combined.Title = "Расписание НФ НИТУ МИСИС";
            combined.ParsedAt = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

            HashSet<string> lessonSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in paths)
            {
                ScheduleDocument document = ParseFile(path);
                if (string.IsNullOrWhiteSpace(combined.WeekLabel))
                {
                    combined.WeekLabel = document.WeekLabel;
                }

                if (!string.IsNullOrWhiteSpace(document.Title) && combined.Title == "Расписание НФ НИТУ МИСИС")
                {
                    combined.Title = document.Title;
                }

                foreach (string source in document.SourceFiles)
                {
                    AddUnique(combined.SourceFiles, source);
                }

                foreach (Lesson lesson in document.Lessons)
                {
                    string signature = string.Join("|", new string[]
                    {
                        lesson.Group,
                        lesson.Subgroup,
                        lesson.Date,
                        lesson.PairNumber.ToString(CultureInfo.InvariantCulture),
                        lesson.TimeRange,
                        lesson.Subject,
                        lesson.LessonType,
                        lesson.Teacher,
                        lesson.Room
                    });

                    if (lessonSignatures.Add(signature))
                    {
                        combined.Lessons.Add(lesson);
                    }
                }

                foreach (DigitalTool tool in document.DigitalTools)
                {
                    MergeDigitalTool(combined.DigitalTools, tool);
                }
            }

            RebuildIndexes(combined);
            return combined;
        }

        private static void ParseSheet(WorksheetData sheet, string path, ScheduleDocument document)
        {
            int headerRow = FindHeaderRow(sheet);
            if (headerRow == 0)
            {
                return;
            }

            string weekLabel = Clean(sheet.GetTextRaw(1, 1));
            string title = Clean(sheet.GetTextRaw(2, 1));
            if (!string.IsNullOrWhiteSpace(weekLabel) && string.IsNullOrWhiteSpace(document.WeekLabel))
            {
                document.WeekLabel = weekLabel;
            }

            if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(document.Title))
            {
                document.Title = title;
            }

            List<GroupColumn> groupColumns = BuildGroupColumns(sheet, headerRow);
            if (groupColumns.Count == 0)
            {
                return;
            }

            string currentDate = null;
            string currentDay = null;

            for (int row = headerRow + 2; row <= sheet.MaxRow; row++)
            {
                string dateCell = Clean(sheet.GetTextRaw(row, 1));
                if (!string.IsNullOrWhiteSpace(dateCell))
                {
                    ParseDateCell(dateCell, out currentDate, out currentDay);
                }

                string pairText = Clean(sheet.GetTextRaw(row, 2));
                int pairNumber;
                if (!int.TryParse(pairText, NumberStyles.Integer, CultureInfo.InvariantCulture, out pairNumber))
                {
                    continue;
                }

                string timeRange = Clean(sheet.GetTextRaw(row, 3));
                if (string.IsNullOrWhiteSpace(currentDate) || string.IsNullOrWhiteSpace(timeRange))
                {
                    continue;
                }

                string startTime;
                string endTime;
                ParseTimeRange(timeRange, out startTime, out endTime);

                foreach (GroupColumn column in groupColumns)
                {
                    string rawLesson = CleanLessonText(sheet.GetText(row, column.SubjectColumn, true));
                    if (string.IsNullOrWhiteSpace(rawLesson))
                    {
                        continue;
                    }

                    string room = Clean(sheet.GetText(row, column.RoomColumn, true));
                    string color = FirstMeaningfulColor(
                        sheet.GetColor(row, column.SubjectColumn, true),
                        sheet.GetColor(row, column.RoomColumn, true));

                    ParsedLessonText parsed = ParseLessonText(rawLesson);
                    Lesson lesson = new Lesson();
                    lesson.Id = BuildLessonId(Path.GetFileName(path), sheet.Name, row, column.SubjectColumn);
                    lesson.Group = column.Group;
                    lesson.Subgroup = column.Subgroup;
                    lesson.Date = currentDate;
                    lesson.DayName = currentDay;
                    lesson.PairNumber = pairNumber;
                    lesson.TimeRange = timeRange;
                    lesson.StartTime = startTime;
                    lesson.EndTime = endTime;
                    lesson.Subject = parsed.Subject;
                    lesson.LessonType = parsed.LessonType;
                    lesson.Teacher = parsed.Teacher;
                    lesson.Room = room;
                    lesson.IsRemote = IsRemoteLesson(room, rawLesson);
                    lesson.ColorHex = color;
                    lesson.SheetName = sheet.Name;
                    lesson.Row = row;
                    lesson.Column = column.SubjectColumn;
                    document.Lessons.Add(lesson);
                }
            }
        }

        private static void ParseDigitalTools(WorksheetData sheet, ScheduleDocument document)
        {
            string firstCell = Clean(sheet.GetTextRaw(1, 1));
            string name = sheet.Name == null ? string.Empty : sheet.Name;
            if (name.IndexOf("циф", StringComparison.OrdinalIgnoreCase) < 0 &&
                firstCell.IndexOf("циф", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            for (int row = 2; row <= sheet.MaxRow; row++)
            {
                string teacher = Clean(sheet.GetTextRaw(row, 1));
                string connection = Clean(sheet.GetTextRaw(row, 2));
                if (string.IsNullOrWhiteSpace(teacher))
                {
                    continue;
                }

                DigitalTool tool = new DigitalTool();
                tool.Teacher = teacher;
                tool.ConnectionInfo = connection;
                MergeDigitalTool(document.DigitalTools, tool);
            }
        }

        private static int FindHeaderRow(WorksheetData sheet)
        {
            for (int row = 1; row <= Math.Min(sheet.MaxRow, 15); row++)
            {
                string col1 = Clean(sheet.GetTextRaw(row, 1));
                string col2 = Clean(sheet.GetTextRaw(row, 2));
                string col3 = Clean(sheet.GetTextRaw(row, 3));
                if (IsSameHeader(col1, "Дата") && IsSameHeader(col2, "№") && IsSameHeader(col3, "Время"))
                {
                    return row;
                }
            }

            return 0;
        }

        private static List<GroupColumn> BuildGroupColumns(WorksheetData sheet, int headerRow)
        {
            List<GroupColumn> result = new List<GroupColumn>();
            int col = 4;
            while (col <= sheet.MaxColumn)
            {
                string header = Clean(sheet.GetTextRaw(headerRow, col));
                if (!IsGroupHeader(header))
                {
                    col++;
                    continue;
                }

                string firstSubgroup = Clean(sheet.GetTextRaw(headerRow + 1, col));
                if (IsSubgroupHeader(firstSubgroup))
                {
                    int cursor = col;
                    while (cursor <= sheet.MaxColumn)
                    {
                        string nextGroupHeader = Clean(sheet.GetTextRaw(headerRow, cursor));
                        if (cursor > col && IsGroupHeader(nextGroupHeader))
                        {
                            break;
                        }

                        string subgroup = Clean(sheet.GetTextRaw(headerRow + 1, cursor));
                        if (IsSubgroupHeader(subgroup))
                        {
                            int roomColumn = FindSubgroupRoomColumn(sheet, headerRow + 1, cursor);
                            GroupColumn groupColumn = new GroupColumn();
                            groupColumn.Group = header;
                            groupColumn.Subgroup = subgroup;
                            groupColumn.SubjectColumn = cursor;
                            groupColumn.RoomColumn = roomColumn;
                            result.Add(groupColumn);
                            cursor = Math.Max(roomColumn + 1, cursor + 1);
                        }
                        else
                        {
                            cursor++;
                        }
                    }

                    col = cursor;
                }
                else
                {
                    int roomColumn = FindGeneralRoomColumn(sheet, headerRow, col);
                    GroupColumn groupColumn = new GroupColumn();
                    groupColumn.Group = header;
                    groupColumn.Subgroup = "Общая";
                    groupColumn.SubjectColumn = col;
                    groupColumn.RoomColumn = roomColumn;
                    result.Add(groupColumn);
                    col = Math.Max(roomColumn + 1, col + 1);
                }
            }

            return result;
        }

        private static int FindSubgroupRoomColumn(WorksheetData sheet, int subgroupRow, int subjectColumn)
        {
            for (int col = subjectColumn + 1; col <= Math.Min(sheet.MaxColumn, subjectColumn + 3); col++)
            {
                string value = Clean(sheet.GetTextRaw(subgroupRow, col));
                if (IsAuditoriumHeader(value))
                {
                    return col;
                }
            }

            return subjectColumn + 1;
        }

        private static int FindGeneralRoomColumn(WorksheetData sheet, int headerRow, int subjectColumn)
        {
            for (int col = subjectColumn + 1; col <= Math.Min(sheet.MaxColumn, subjectColumn + 6); col++)
            {
                string value = Clean(sheet.GetTextRaw(headerRow, col));
                if (IsAuditoriumHeader(value))
                {
                    return col;
                }

                if (IsGroupHeader(value))
                {
                    break;
                }
            }

            return subjectColumn + 1;
        }

        private static bool IsSameHeader(string value, string expected)
        {
            return string.Equals(Clean(value), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAuditoriumHeader(string value)
        {
            string cleaned = Clean(value).TrimEnd('.');
            return string.Equals(cleaned, "Ауд", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSubgroupHeader(string value)
        {
            return Clean(value).StartsWith("подгруппа", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGroupHeader(string value)
        {
            string cleaned = Clean(value);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return false;
            }

            if (IsAuditoriumHeader(cleaned) ||
                IsSubgroupHeader(cleaned) ||
                IsSameHeader(cleaned, "Дата") ||
                IsSameHeader(cleaned, "№") ||
                IsSameHeader(cleaned, "Время"))
            {
                return false;
            }

            return GroupRegex.IsMatch(cleaned);
        }

        private static void ParseDateCell(string text, out string date, out string day)
        {
            date = null;
            day = null;

            Match match = DateRegex.Match(text);
            if (!match.Success)
            {
                return;
            }

            DateTime parsed;
            if (DateTime.TryParseExact(match.Groups["date"].Value, new string[] { "dd.MM.yyyy", "d.M.yyyy" }, new CultureInfo("ru-RU"), DateTimeStyles.None, out parsed))
            {
                date = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            day = Clean(match.Groups["day"].Value);
        }

        private static void ParseTimeRange(string text, out string startTime, out string endTime)
        {
            startTime = string.Empty;
            endTime = string.Empty;

            Match match = TimeRegex.Match(text);
            if (!match.Success)
            {
                return;
            }

            int h1 = int.Parse(match.Groups["h1"].Value, CultureInfo.InvariantCulture);
            int m1 = int.Parse(match.Groups["m1"].Value, CultureInfo.InvariantCulture);
            int h2 = int.Parse(match.Groups["h2"].Value, CultureInfo.InvariantCulture);
            int m2 = int.Parse(match.Groups["m2"].Value, CultureInfo.InvariantCulture);
            startTime = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", h1, m1);
            endTime = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", h2, m2);
        }

        private static ParsedLessonText ParseLessonText(string text)
        {
            ParsedLessonText parsed = new ParsedLessonText();
            string normalized = CleanLessonText(text);
            Match teacherMatch = TeacherRegex.Match(normalized);
            if (teacherMatch.Success)
            {
                parsed.Teacher = NormalizeTeacher(teacherMatch.Groups["teacher"].Value);
                normalized = normalized.Substring(0, teacherMatch.Index).Trim();
                normalized = normalized.TrimEnd('/').Trim();
            }

            MatchCollection typeMatches = Regex.Matches(normalized, @"\(([^()]*)\)");
            if (typeMatches.Count > 0)
            {
                Match last = typeMatches[typeMatches.Count - 1];
                parsed.LessonType = Clean(last.Groups[1].Value);
                normalized = normalized.Remove(last.Index, last.Length).Trim();
            }

            parsed.Subject = normalized.Trim().TrimEnd('/').Trim();
            if (string.IsNullOrWhiteSpace(parsed.Subject))
            {
                parsed.Subject = CleanLessonText(text);
            }

            return parsed;
        }

        private static string NormalizeTeacher(string teacher)
        {
            string value = Clean(teacher);
            value = Regex.Replace(value, @"\s+([А-ЯЁ])\.", " $1.");
            value = Regex.Replace(value, @"([А-ЯЁ])\s*\.?\s*([А-ЯЁ])\.?$", "$1.$2.");
            return value.Trim();
        }

        private static bool IsRemoteLesson(string room, string lessonText)
        {
            string combined = (room + " " + lessonText).ToLowerInvariant();
            return combined.Contains("zoom") ||
                   combined.Contains("teams") ||
                   combined.Contains("meet") ||
                   combined.Contains("skype") ||
                   combined.Contains("онлайн") ||
                   combined.Contains("дистанц");
        }

        private static string FirstMeaningfulColor(params string[] colors)
        {
            foreach (string color in colors)
            {
                if (IsMeaningfulColor(color))
                {
                    return color;
                }
            }

            return string.Empty;
        }

        private static bool IsMeaningfulColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return false;
            }

            string cleaned = color.Trim().ToUpperInvariant();
            return cleaned != "#FFFFFF" && cleaned != "#000000";
        }

        private static string BuildLessonId(string fileName, string sheetName, int row, int column)
        {
            string raw = fileName + "|" + sheetName + "|" + row.ToString(CultureInfo.InvariantCulture) + "|" + column.ToString(CultureInfo.InvariantCulture);
            return Math.Abs(raw.GetHashCode()).ToString("X8", CultureInfo.InvariantCulture);
        }

        private static string CleanLessonText(string text)
        {
            return Regex.Replace(Clean(text), @"\s+", " ").Trim();
        }

        private static string Clean(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }

            return text.Replace('\u00A0', ' ').Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static void RebuildIndexes(ScheduleDocument document)
        {
            HashSet<string> groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> teachers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Lesson lesson in document.Lessons)
            {
                if (!string.IsNullOrWhiteSpace(lesson.Group))
                {
                    groups.Add(lesson.Group);
                }

                if (!string.IsNullOrWhiteSpace(lesson.Teacher))
                {
                    teachers.Add(lesson.Teacher);
                }
            }

            foreach (DigitalTool tool in document.DigitalTools)
            {
                if (!string.IsNullOrWhiteSpace(tool.Teacher))
                {
                    teachers.Add(tool.Teacher);
                }
            }

            document.Groups = groups.OrderBy(x => x).ToList();
            document.Teachers = teachers.OrderBy(x => x).ToList();
            document.Lessons = document.Lessons
                .OrderBy(x => x.Date)
                .ThenBy(x => x.PairNumber)
                .ThenBy(x => x.Group)
                .ThenBy(x => x.Subgroup)
                .ToList();
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (string existing in values)
            {
                if (string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            values.Add(value);
        }

        private static void MergeDigitalTool(List<DigitalTool> tools, DigitalTool tool)
        {
            if (tool == null || string.IsNullOrWhiteSpace(tool.Teacher))
            {
                return;
            }

            foreach (DigitalTool existing in tools)
            {
                if (string.Equals(existing.Teacher, tool.Teacher, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(existing.ConnectionInfo) && !string.IsNullOrWhiteSpace(tool.ConnectionInfo))
                    {
                        existing.ConnectionInfo = tool.ConnectionInfo;
                    }

                    return;
                }
            }

            tools.Add(tool);
        }

        private static WorkbookData ReadWorkbook(ZipArchive archive)
        {
            List<string> sharedStrings = ReadSharedStrings(archive);
            List<string> fillColors = ReadStyleFillColors(archive);
            List<int> styleFillIndexes = ReadStyleFillIndexes(archive);
            Dictionary<string, string> relationships = ReadWorkbookRelationships(archive);

            XDocument workbookXml = ReadXml(archive, "xl/workbook.xml");
            WorkbookData workbook = new WorkbookData();

            foreach (XElement sheetElement in workbookXml.Descendants(SpreadsheetNs + "sheet"))
            {
                string name = (string)sheetElement.Attribute("name");
                string relationshipId = (string)sheetElement.Attribute(RelationshipNs + "id");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationshipId) || !relationships.ContainsKey(relationshipId))
                {
                    continue;
                }

                string sheetPath = relationships[relationshipId];
                WorksheetData sheet = ReadWorksheet(archive, sheetPath, name, sharedStrings, fillColors, styleFillIndexes);
                workbook.Worksheets.Add(sheet);
            }

            return workbook;
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            List<string> values = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return values;
            }

            XDocument document = ReadXml(entry);
            foreach (XElement item in document.Descendants(SpreadsheetNs + "si"))
            {
                string text = string.Concat(item.Descendants(SpreadsheetNs + "t").Select(x => (string)x));
                values.Add(text);
            }

            return values;
        }

        private static List<string> ReadStyleFillColors(ZipArchive archive)
        {
            List<string> colors = new List<string>();
            ZipArchiveEntry entry = archive.GetEntry("xl/styles.xml");
            if (entry == null)
            {
                return colors;
            }

            XDocument document = ReadXml(entry);
            XElement fillsElement = document.Root.Element(SpreadsheetNs + "fills");
            if (fillsElement == null)
            {
                return colors;
            }

            foreach (XElement fill in fillsElement.Elements(SpreadsheetNs + "fill"))
            {
                string color = null;
                XElement pattern = fill.Element(SpreadsheetNs + "patternFill");
                if (pattern != null)
                {
                    XElement fg = pattern.Element(SpreadsheetNs + "fgColor");
                    XElement bg = pattern.Element(SpreadsheetNs + "bgColor");
                    color = NormalizeRgb((string)(fg == null ? null : fg.Attribute("rgb")));
                    if (string.IsNullOrWhiteSpace(color))
                    {
                        color = NormalizeRgb((string)(bg == null ? null : bg.Attribute("rgb")));
                    }
                }

                colors.Add(color ?? string.Empty);
            }

            return colors;
        }

        private static List<int> ReadStyleFillIndexes(ZipArchive archive)
        {
            List<int> indexes = new List<int>();
            ZipArchiveEntry entry = archive.GetEntry("xl/styles.xml");
            if (entry == null)
            {
                return indexes;
            }

            XDocument document = ReadXml(entry);
            XElement cellXfs = document.Root.Element(SpreadsheetNs + "cellXfs");
            if (cellXfs == null)
            {
                return indexes;
            }

            foreach (XElement xf in cellXfs.Elements(SpreadsheetNs + "xf"))
            {
                int fillId;
                if (int.TryParse((string)xf.Attribute("fillId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out fillId))
                {
                    indexes.Add(fillId);
                }
                else
                {
                    indexes.Add(0);
                }
            }

            return indexes;
        }

        private static string NormalizeRgb(string rgb)
        {
            if (string.IsNullOrWhiteSpace(rgb))
            {
                return string.Empty;
            }

            string value = rgb.Trim().TrimStart('#').ToUpperInvariant();
            if (value.Length == 8)
            {
                value = value.Substring(2);
            }

            if (value.Length != 6)
            {
                return string.Empty;
            }

            return "#" + value;
        }

        private static Dictionary<string, string> ReadWorkbookRelationships(ZipArchive archive)
        {
            Dictionary<string, string> relationships = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ZipArchiveEntry entry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (entry == null)
            {
                return relationships;
            }

            XDocument document = ReadXml(entry);
            foreach (XElement relationship in document.Root.Elements())
            {
                string id = (string)relationship.Attribute("Id");
                string target = (string)relationship.Attribute("Target");
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                target = target.Replace('\\', '/');
                if (target.StartsWith("/", StringComparison.Ordinal))
                {
                    target = target.TrimStart('/');
                }
                else
                {
                    target = "xl/" + target;
                }

                relationships[id] = target;
            }

            return relationships;
        }

        private static WorksheetData ReadWorksheet(ZipArchive archive, string path, string name, List<string> sharedStrings, List<string> fillColors, List<int> styleFillIndexes)
        {
            XDocument document = ReadXml(archive, path);
            WorksheetData sheet = new WorksheetData();
            sheet.Name = name;

            foreach (XElement cellElement in document.Descendants(SpreadsheetNs + "c"))
            {
                string reference = (string)cellElement.Attribute("r");
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                int row;
                int column;
                ParseCellReference(reference, out row, out column);
                if (row == 0 || column == 0)
                {
                    continue;
                }

                int styleIndex = 0;
                int.TryParse((string)cellElement.Attribute("s"), NumberStyles.Integer, CultureInfo.InvariantCulture, out styleIndex);

                CellData cell = new CellData();
                cell.Text = ReadCellText(cellElement, sharedStrings);
                cell.ColorHex = ResolveCellColor(styleIndex, fillColors, styleFillIndexes);
                sheet.SetCell(row, column, cell);
            }

            foreach (XElement mergeCell in document.Descendants(SpreadsheetNs + "mergeCell"))
            {
                string reference = (string)mergeCell.Attribute("ref");
                MergedRange range;
                if (MergedRange.TryParse(reference, out range))
                {
                    sheet.MergedRanges.Add(range);
                }
            }

            return sheet;
        }

        private static string ReadCellText(XElement cellElement, List<string> sharedStrings)
        {
            string type = (string)cellElement.Attribute("t");
            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
            {
                string rawIndex = (string)cellElement.Element(SpreadsheetNs + "v");
                int index;
                if (int.TryParse(rawIndex, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) && index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }

                return string.Empty;
            }

            if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat(cellElement.Descendants(SpreadsheetNs + "t").Select(x => (string)x));
            }

            XElement valueElement = cellElement.Element(SpreadsheetNs + "v");
            if (valueElement == null)
            {
                return string.Empty;
            }

            return (string)valueElement;
        }

        private static string ResolveCellColor(int styleIndex, List<string> fillColors, List<int> styleFillIndexes)
        {
            if (styleIndex < 0 || styleIndex >= styleFillIndexes.Count)
            {
                return string.Empty;
            }

            int fillIndex = styleFillIndexes[styleIndex];
            if (fillIndex < 0 || fillIndex >= fillColors.Count)
            {
                return string.Empty;
            }

            return fillColors[fillIndex];
        }

        private static XDocument ReadXml(ZipArchive archive, string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path);
            if (entry == null)
            {
                throw new InvalidDataException("В XLSX не найден файл " + path + ".");
            }

            return ReadXml(entry);
        }

        private static XDocument ReadXml(ZipArchiveEntry entry)
        {
            using (Stream stream = entry.Open())
            {
                return XDocument.Load(stream);
            }
        }

        private static void ParseCellReference(string reference, out int row, out int column)
        {
            row = 0;
            column = 0;

            int index = 0;
            while (index < reference.Length && char.IsLetter(reference[index]))
            {
                column = column * 26 + (char.ToUpperInvariant(reference[index]) - 'A' + 1);
                index++;
            }

            string rowText = reference.Substring(index);
            int.TryParse(rowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out row);
        }

        private sealed class WorkbookData
        {
            public WorkbookData()
            {
                Worksheets = new List<WorksheetData>();
            }

            public List<WorksheetData> Worksheets { get; private set; }
        }

        private sealed class WorksheetData
        {
            private readonly Dictionary<long, CellData> _cells;

            public WorksheetData()
            {
                _cells = new Dictionary<long, CellData>();
                MergedRanges = new List<MergedRange>();
            }

            public string Name { get; set; }
            public int MaxRow { get; private set; }
            public int MaxColumn { get; private set; }
            public List<MergedRange> MergedRanges { get; private set; }

            public void SetCell(int row, int column, CellData cell)
            {
                _cells[Key(row, column)] = cell;
                if (row > MaxRow)
                {
                    MaxRow = row;
                }

                if (column > MaxColumn)
                {
                    MaxColumn = column;
                }
            }

            public string GetTextRaw(int row, int column)
            {
                CellData cell = GetCellRaw(row, column);
                return cell == null ? string.Empty : cell.Text;
            }

            public string GetText(int row, int column, bool useMerged)
            {
                CellData cell = GetCellRaw(row, column);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.Text))
                {
                    return cell.Text;
                }

                if (useMerged)
                {
                    CellData merged = GetMergedTopLeftCell(row, column);
                    if (merged != null)
                    {
                        return merged.Text;
                    }
                }

                return string.Empty;
            }

            public string GetColor(int row, int column, bool useMerged)
            {
                CellData cell = GetCellRaw(row, column);
                if (cell != null && !string.IsNullOrWhiteSpace(cell.ColorHex))
                {
                    return cell.ColorHex;
                }

                if (useMerged)
                {
                    CellData merged = GetMergedTopLeftCell(row, column);
                    if (merged != null)
                    {
                        return merged.ColorHex;
                    }
                }

                return string.Empty;
            }

            private CellData GetMergedTopLeftCell(int row, int column)
            {
                foreach (MergedRange range in MergedRanges)
                {
                    if (range.Contains(row, column))
                    {
                        return GetCellRaw(range.StartRow, range.StartColumn);
                    }
                }

                return null;
            }

            private CellData GetCellRaw(int row, int column)
            {
                CellData cell;
                if (_cells.TryGetValue(Key(row, column), out cell))
                {
                    return cell;
                }

                return null;
            }

            private static long Key(int row, int column)
            {
                return ((long)row << 20) + column;
            }
        }

        private sealed class CellData
        {
            public string Text { get; set; }
            public string ColorHex { get; set; }
        }

        private sealed class ParsedLessonText
        {
            public string Subject { get; set; }
            public string LessonType { get; set; }
            public string Teacher { get; set; }
        }

        private sealed class MergedRange
        {
            public int StartRow { get; private set; }
            public int StartColumn { get; private set; }
            public int EndRow { get; private set; }
            public int EndColumn { get; private set; }

            public bool Contains(int row, int column)
            {
                return row >= StartRow && row <= EndRow && column >= StartColumn && column <= EndColumn;
            }

            public static bool TryParse(string reference, out MergedRange range)
            {
                range = null;
                if (string.IsNullOrWhiteSpace(reference))
                {
                    return false;
                }

                string[] parts = reference.Split(':');
                if (parts.Length == 0)
                {
                    return false;
                }

                int startRow;
                int startColumn;
                int endRow;
                int endColumn;
                ParseCellReference(parts[0], out startRow, out startColumn);
                if (parts.Length > 1)
                {
                    ParseCellReference(parts[1], out endRow, out endColumn);
                }
                else
                {
                    endRow = startRow;
                    endColumn = startColumn;
                }

                if (startRow == 0 || startColumn == 0 || endRow == 0 || endColumn == 0)
                {
                    return false;
                }

                range = new MergedRange();
                range.StartRow = Math.Min(startRow, endRow);
                range.StartColumn = Math.Min(startColumn, endColumn);
                range.EndRow = Math.Max(startRow, endRow);
                range.EndColumn = Math.Max(startColumn, endColumn);
                return true;
            }
        }
    }
}
