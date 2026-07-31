using System;
using System.Collections.Generic;

namespace ScheduleParser.Core
{
    public static class SchedulePublicationHelper
    {
        public const string DefaultUnavailableMessage = "Расписание сейчас недоступно. Учебный отдел еще не опубликовал файлы.";

        public static ScheduleDocument PrepareForPublication(ScheduleDocument document)
        {
            if (document == null)
            {
                return CreateUnavailableDocument(DefaultUnavailableMessage);
            }

            EnsureLists(document);
            document.HeartbeatAt = CurrentTimestamp();
            if (document.Lessons.Count > 0)
            {
                document.UnavailableMessage = null;
            }
            else if (string.IsNullOrWhiteSpace(document.UnavailableMessage))
            {
                document.UnavailableMessage = DefaultUnavailableMessage;
            }

            return document;
        }

        public static ScheduleDocument CreateUnavailableDocument(string message)
        {
            ScheduleDocument document = new ScheduleDocument();
            document.Title = "Расписание недоступно";
            document.ParsedAt = CurrentTimestamp();
            document.HeartbeatAt = document.ParsedAt;
            document.UnavailableMessage = string.IsNullOrWhiteSpace(message) ? DefaultUnavailableMessage : message;
            return document;
        }

        public static ScheduleDocument RefreshHeartbeat(ScheduleDocument document)
        {
            if (document == null)
            {
                return CreateUnavailableDocument(DefaultUnavailableMessage);
            }

            EnsureLists(document);
            document.HeartbeatAt = CurrentTimestamp();
            return document;
        }

        private static void EnsureLists(ScheduleDocument document)
        {
            if (document.SourceFiles == null) document.SourceFiles = new List<string>();
            if (document.Courses == null) document.Courses = new List<string>();
            if (document.WeekTypes == null) document.WeekTypes = new List<string>();
            if (document.Groups == null) document.Groups = new List<string>();
            if (document.Teachers == null) document.Teachers = new List<string>();
            if (document.Lessons == null) document.Lessons = new List<Lesson>();
            if (document.DigitalTools == null) document.DigitalTools = new List<DigitalTool>();
        }

        private static string CurrentTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}
