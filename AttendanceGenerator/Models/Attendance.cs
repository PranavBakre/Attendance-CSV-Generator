using System;

namespace AttendanceGenerator.Models
{
    public class Attendance
    {
        public string PRN { get; set; }
        public string RollNo { get; set; }
        public string Name { get; set; }
        public string PreviousAction { get; set; }
        public int UnbalancedJoins { get; set; }
        public TimeSpan JoinTime { get; set; }
        public TimeSpan LeaveTime { get; set; }
        public TimeSpan Time { get { return LeaveTime - JoinTime; } }
    }
}
