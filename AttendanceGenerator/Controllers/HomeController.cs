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
        public IActionResult Index(string panel, IFormFile file, TimeSpan endTime)
        {
            _logger.LogInformation(panel + "\t" + file.FileName + "\t" + file.ContentType);

            var attendance = GenerateAttendanceRecord(panel, file, endTime);
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine($"PRN,Name,Time attended");
            foreach (var x in attendance.OrderBy(x => x.PRN))
            {
                writer.WriteLine($"{x.PRN},\"{x.Name}\",{x.Time}");
            }
            stream.Position = 0;
            return File(stream, "application/vnd.ms-excel", "Attendance.csv");
        }

        [NonAction]
        public IEnumerable<Attendance> GenerateAttendanceRecord(string panel, IFormFile file, TimeSpan endTime)
        {
            var meeting = ReadMeetingAttendanceRecord(file.OpenReadStream());
            var panelList = ReadClassRecord(panel);
            _logger.LogInformation($"{panelList.Count()}");
            var attendance = new List<Attendance>();
            foreach (var entry in meeting)
            {
                if (attendance.Where(x => entry.Name.Contains(x.Name)).Count() == 0)
                {
                    _logger.LogInformation($"{panelList.Count()}");
                    attendance.Add(new Attendance
                    {
                        Name = entry.Name,
                        PRN = panelList.Where(x => entry.Name.Contains(x.Name, StringComparison.OrdinalIgnoreCase)).Select(x => x.PRN).FirstOrDefault(),
                        Time = TimeSpan.Zero
                    });
                }

                if (entry.Action.Contains("Joined"))
                {
                    attendance.Where(x => entry.Name.Contains(x.Name)).First().Time -= entry.TimeStamp.TimeOfDay;
                }
                else
                {
                    attendance.Where(x => entry.Name.Contains(x.Name)).First().Time += entry.TimeStamp.TimeOfDay;
                }

            }
            foreach (var x in attendance)
            {
                if (x.Time.CompareTo(TimeSpan.Zero) < 0)
                {
                    x.Time = x.Time.Add(endTime);
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
