using System;
using System.Collections.Generic;

namespace ScheduleParser.Core
{
    public sealed class ScheduleDocument
    {
        public ScheduleDocument()
        {
            Courses = new List<string>();
            WeekTypes = new List<string>();
            Groups = new List<string>();
            Teachers = new List<string>();
            Lessons = new List<Lesson>();
            DigitalTools = new List<DigitalTool>();
            SourceFiles = new List<string>();
        }

        public string Title { get; set; }
        public string WeekLabel { get; set; }
        public string ParsedAt { get; set; }
        public List<string> SourceFiles { get; set; }
        public List<string> Courses { get; set; }
        public List<string> WeekTypes { get; set; }
        public List<string> Groups { get; set; }
        public List<string> Teachers { get; set; }
        public List<Lesson> Lessons { get; set; }
        public List<DigitalTool> DigitalTools { get; set; }
    }

    public sealed class Lesson
    {
        public string Id { get; set; }
        public string Course { get; set; }
        public string WeekType { get; set; }
        public string Group { get; set; }
        public string Subgroup { get; set; }
        public string Date { get; set; }
        public string DayName { get; set; }
        public int PairNumber { get; set; }
        public string TimeRange { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Subject { get; set; }
        public string LessonType { get; set; }
        public string Teacher { get; set; }
        public string Room { get; set; }
        public bool IsRemote { get; set; }
        public string ColorHex { get; set; }
        public string SheetName { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }

    public sealed class DigitalTool
    {
        public string Teacher { get; set; }
        public string ConnectionInfo { get; set; }
    }

    internal sealed class GroupColumn
    {
        public string Group { get; set; }
        public string Subgroup { get; set; }
        public int SubjectColumn { get; set; }
        public int RoomColumn { get; set; }
    }
}
