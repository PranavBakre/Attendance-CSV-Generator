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

            var attendance = GenerateAttendanceRecord(panel, file, endTime,time);
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write($"Roll No,Name,PRN");
            if (time)
            {
                writer.Write(",Time attended");
            }
            writer.WriteLine();
            foreach (var x in attendance.OrderBy(x => x.PRN))
            {
                writer.Write($"{x.RollNo},\"{x.Name}\",{x.PRN}");
                if(time)
                {
                    var multiplejoins = (x.UnbalancedJoins > 0 ? ",Multiple Joins detected" : "");
                    writer.Write($",{ x.Time}{multiplejoins}");
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
                var attendee = attendance.Where(x => entry.Name.Contains(x.Name));
                if (attendee.Count() == 0)
                {
                    var panelEntry = panelList.Where(x => entry.Name.Contains(x.Name, StringComparison.OrdinalIgnoreCase));
                    attendance.Add(new Attendance
                    {
                        Name = entry.Name,
                        RollNo = panelEntry.Select(x => x.RollNo).FirstOrDefault(),
                        PRN = panelEntry.Select(x => x.PRN).FirstOrDefault(),
                        Time = TimeSpan.Zero,
                        UnbalancedJoins = 0
                    });
                }
                if (time)
                {
                    if (entry.Action.Contains("Joined"))
                    {
                        attendee.First().Time -= entry.TimeStamp.TimeOfDay;
                        if (attendee.First().PreviousAction=="Joined")
                            attendee.First().UnbalancedJoins= attendee.First().UnbalancedJoins+1;
                        attendee.First().PreviousAction = "Joined";
                    }
                    else
                    {
                        attendee.First().Time += entry.TimeStamp.TimeOfDay;
                        attendee.First().PreviousAction="Left";
                    }
                }
            }
            if (time)
            { 
            foreach (var x in attendance)
            {
                if (x.Time.CompareTo(TimeSpan.Zero) < 0)
                {
                    x.Time = x.Time.Add(endTime);
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
            var streamReader = new StreamReader($"{_env.WebRootPath}/Panels/{panel}.csv");
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
