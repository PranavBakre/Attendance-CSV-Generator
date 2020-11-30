using AttendanceGenerator.Models;
using CsvHelper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AttendanceGenerator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Index(string panel, IFormFile file, TimeSpan endTime, bool time)
        {
            _logger.LogInformation(panel + "\t" + file.FileName + "\t" + file.ContentType);

            var attendance = GenerateAttendanceRecord(panel, file, endTime, time);
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write($"Roll No,Name,PRN");
            if (time)
            {
                writer.Write(",Time attended");
            }
            writer.WriteLine();
            foreach (var x in attendance.OrderBy(x => x.RollNo))
            {
                writer.Write($"{x.RollNo},\"{x.Name}\",{x.PRN}");
                if (time)
                {
                    writer.Write($",{ x.Time}");
                }
                writer.WriteLine();
            }
            writer.Flush();
            stream.Position = 0;
            return File(stream, "text/csv", "Attendance.csv");
        }

        [NonAction]
        public IEnumerable<Attendance> GenerateAttendanceRecord(string panel, IFormFile file, TimeSpan endTime, bool time)
        {
            var meeting = ReadMeetingAttendanceRecord(file.OpenReadStream());
            var panelList = ReadClassRecord(panel);
            _logger.LogInformation($"{panelList.Count()}");
            var attendance = new List<Attendance>();
            foreach (var entry in meeting)
            {
                var attendee = attendance.Where(x => entry.Name.Contains(x.Name)).FirstOrDefault();
                if (attendee == null)
                {
                    var panelEntry = panelList.Where(x => entry.Name.Replace(" ",string.Empty).Contains(x.Name.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    attendance.Add(new Attendance
                    {
                        Name = entry.Name,
                        RollNo = panelEntry != null ? panelEntry.RollNo : "",
                        PRN = panelEntry != null ? panelEntry.PRN : "",
                        UnbalancedJoins = 0
                    });
                }
                attendee ??= attendance.Last();
                if (time)
                {
                    if (entry.Action.Contains("Joined"))
                    {

                        if (attendee.PreviousAction == "Joined")
                            attendee.UnbalancedJoins++;
                        else
                        {
                            attendee.JoinTime = entry.TimeStamp.TimeOfDay - attendee.Time;
                        }
                        attendee.PreviousAction = "Joined";
                    }
                    else
                    {
                        if (attendee.PreviousAction == "Left")
                        {
                            attendee.UnbalancedJoins--;
                        }

                        attendee.LeaveTime = entry.TimeStamp.TimeOfDay;
                        attendee.PreviousAction = "Left";
                    }
                }
            }
            if (time)
            {
                foreach (var x in attendance)
                {
                    if (x.PreviousAction == "Joined" || x.UnbalancedJoins != 0)
                    {
                        x.LeaveTime = endTime;
                    }
                }
            }
            return attendance;

        }

        [NonAction]
        public IEnumerable<MeetingAttendance> ReadMeetingAttendanceRecord(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            var reader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            reader.Configuration.Delimiter = "\t";
            return reader.GetRecords<MeetingAttendance>();

        }

        [NonAction]
        public IEnumerable<Panel> ReadClassRecord(string panel)
        {
            var streamReader = new StreamReader($"{_env.ContentRootPath}/Data/Panels/{panel}.csv");
            var reader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            reader.Configuration.HasHeaderRecord = false;
            return reader.GetRecords<Panel>().ToList();

        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
