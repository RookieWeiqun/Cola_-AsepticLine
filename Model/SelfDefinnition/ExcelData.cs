namespace Cola.Model
{
    public class ExcelData
    {
        public string? DeviceName { get; set; }

        public string? ProjectDescription { get; set; }

        public string? ReferenceValue { get; set; }

        public string? Unit { get; set; }

        public string? ProjectName { get; set; }
        // Dictionary to store time-based values
        public Dictionary<string, string> TimeValues { get; set; } = new Dictionary<string, string>();

    }
    public class ExcelAlarmData
    {
        public string? DeviceName { get; set; }
        public string? CheckItem { get; set; }
        public string? SharpTime { get; set; }
        public string? Remark { get; set; }
    }
}
