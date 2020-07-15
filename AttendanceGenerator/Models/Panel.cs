namespace AttendanceGenerator.Models
{
    public class Panel
    {
        [CsvHelper.Configuration.Attributes.NameIndex(0)]
        public string PRN { get; set; }
        [CsvHelper.Configuration.Attributes.NameIndex(1)]
        public string Name { get; set; }
    }
}
