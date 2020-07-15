using System;

namespace AttendanceGenerator.Models
{
    public class Attendance
    {
        public string PRN { get; set; }
        public string Name { get; set; }
        public TimeSpan Time { get; set; }
    }
}
