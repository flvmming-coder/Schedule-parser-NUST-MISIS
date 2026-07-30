using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace ScheduleParser.Core
{
    public static class ScheduleJsonSerializer
    {
        public static string ToJson(ScheduleDocument document)
        {
            JavaScriptSerializer serializer = CreateSerializer();
            return serializer.Serialize(document);
        }

        public static ScheduleDocument FromJson(string json)
        {
            JavaScriptSerializer serializer = CreateSerializer();
            return serializer.Deserialize<ScheduleDocument>(json);
        }

        public static void Save(ScheduleDocument document, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, ToJson(document), new UTF8Encoding(false));
        }

        public static ScheduleDocument Load(string path)
        {
            return FromJson(File.ReadAllText(path, Encoding.UTF8));
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            serializer.RecursionLimit = 100;
            return serializer;
        }
    }
}
