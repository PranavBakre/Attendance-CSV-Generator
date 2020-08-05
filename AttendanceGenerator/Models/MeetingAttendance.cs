using System;
using System.Globalization;

namespace AttendanceGenerator.Models
{
    public class MeetingAttendance
    {
        [CsvHelper.Configuration.Attributes.Name("Full Name")]
        public string Name { get; set; }
        [CsvHelper.Configuration.Attributes.Name("User Action")]
        public string Action { get; set; }
        [CsvHelper.Configuration.Attributes.Name("Timestamp")]
        public string Time { get; set; }
        public DateTime TimeStamp => DateTime.ParseExact(Time.Trim('"'), "M/d/yyyy, h:mm:ss tt", CultureInfo.InvariantCulture);
    }
}
