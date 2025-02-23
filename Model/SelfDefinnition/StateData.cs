namespace Cola.Model.SelfDefinnition
{
    public class StateDataResult
    {
        public long Id { get; set; }
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        //public DateTime? RecordTime { get; set; }
        public DateTime? BeginTime { get; set; }
        public int? Duration { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public float? Weight { get; set; }
        public string? MixerStep { get; set; }
        public int? ProductFlowRate { get; set; }
        public string? Formula { get; set; }
    }
}
