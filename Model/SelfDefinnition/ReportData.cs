namespace Cola.Model
{
    public class RealtimeDataResult
    {
        public int Id { get; set; }
        public int? DeviceId { get; set; }
        public string Name { get; set; }
        public int? LineId { get; set; }
        public int? RecipeId { get; set; }
        public DateTime? RecordTime { get; set; }
        public ReportDataItem Data { get; set; }=new ReportDataItem();
    }
    public class ReportDataItem
    {
        public int Id { get; set; }
        public float Weight { get; set; }
        public string DeviceStatus { get; set; }
        public string CleanStatus { get; set; }
        public string BlendStatus { get; set; }
        public float ProductFlowRate { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }
        public string MixerStep { get; set; }
        public float Temperature { get; set; } 
        public float LiquidLevel { get; set; } 
        public int? Capacity { get; set; }

    }

}
