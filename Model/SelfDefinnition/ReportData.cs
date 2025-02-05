namespace Cola.Model
{
    public class RealtimeDataResult
    {
        public int Id { get; set; }
        public List<DataItem> Data { get; set; } = new List<DataItem>();
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        public int? RecipeId { get; set; }
        public DateTime? UpdateTime { get; set; }
        public DeviceInfo DeviceInfo { get; set; }
    }
    public class DataItem
    {
        public int RealtimeDataId { get; set; }
        public int CheckParaId { get; set; }
        public string CheckParaAliasName { get; set; }
        public object Value { get; set; }
    }
}
