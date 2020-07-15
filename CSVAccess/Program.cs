using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CSVAccess
{
    class Program
    {
        static void Main(string[] args)
        {
            var mlContext = new MLContext();
            var panelView = mlContext.Data.LoadFromTextFile<Class>("CSV/Panel2.csv", ',', hasHeader: false);
            var classData = mlContext.Data.CreateEnumerable<Class>(panelView, true);
            var attendanceView = mlContext.Data.LoadFromTextFile<CSV>("CSV/CSV.csv", hasHeader: true);
            var attendanceEnumerable = mlContext.Data.CreateEnumerable<CSV>(data: attendanceView, reuseRowObject: true);
            var dictionary = new Dictionary<string, AttendanceCSV>();
            var currTime = DateTime.Now;
            foreach (var student in attendanceEnumerable)
            {
                if (!dictionary.ContainsKey(student.Name))
                {
                    dictionary[student.Name] = new AttendanceCSV
                    {
                        PRN = classData.Where(
                            xc => student.Name.Contains(xc.Name, StringComparison.OrdinalIgnoreCase)
                            ).Select(xc => xc.PRN)
                            .FirstOrDefault(),
                        Time = TimeSpan.Zero
                    };
                }
                if (student.Action.Contains("Joined"))
                {
                    dictionary[student.Name].Time = dictionary[student.Name].Time.Subtract(student.TimeStamp.TimeOfDay);
                }
                else
                {
                    dictionary[student.Name].Time = dictionary[student.Name].Time.Add(student.TimeStamp.TimeOfDay);
                }
            }
            var fs = File.Create("Attendance.csv");
            StreamWriter streamWriter = new StreamWriter(fs);
            foreach (var xc in dictionary)
            {
                if ((xc.Value.Time).CompareTo(TimeSpan.Zero) < 0)
                {
                    xc.Value.Time = xc.Value.Time.Add(TimeSpan.Parse("11:30:00"));
                }
                streamWriter.WriteLine($"\"{xc.Key}\",{xc.Value.PRN},{xc.Value.Time}");
            }
            streamWriter.Close();
            fs.Close();
        }
    }
    public class AttendanceCSV
    {
        public string PRN { get; set; }
        public TimeSpan Time { get; set; }
    }
    public class Class
    {
        [LoadColumn(0)]
        public string PRN { get; set; }

        [LoadColumn(1)]
        public string Name { get; set; }
    }
    public class CSV
    {
        [LoadColumn(0)]
        public string Name { get; set; }
        [LoadColumn(1)]
        public string Action { get; set; }
        [LoadColumn(2)]
        public string Time { get; set; }
        [LoadColumn(4)]
        public DateTime TimeStamp => DateTime.ParseExact(Time.Trim('"'), "M/dd/yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);
    }
}
