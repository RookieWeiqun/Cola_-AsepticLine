namespace Cola.Model.SelfDefinnition
{
    public class StateDataResult
    {
        public long Id { get; set; }
        public List<StateDataItem> Data { get; set; } = new List<StateDataItem>();
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        public int? RecipeId { get; set; }
        public DateTime? RecordTime { get; set; }
        public DateTime? BeginTime { get; set; }
        public int? Duration { get; set; }
        public DateTime? EndTime { get; set; }
        public int? StateId { get; set; }
    }
    public class StateDataItem
    {
        public int RealtimeDataId { get; set; }
        public int CheckParaId { get; set; }
        public string CheckParaAliasName { get; set; }
        public object Value { get; set; }
    }
}
