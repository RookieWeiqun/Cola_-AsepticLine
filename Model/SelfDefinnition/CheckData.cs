namespace Cola.Model.SelfDefinnition
{
    public class CheckDataResult
    {
        public int Id { get; set; }
        public List<CheckDataItem> Data { get; set; } = new List<CheckDataItem>();
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        public DateTime? RecordTime { get; set; }
        public int? RecipeId { get; set; }
    }
    public class CheckDataItem
    {
        public int CheckDataId { get; set; }
        public int CheckParaId { get; set; }
        public string CheckParaAliasName { get; set; }
        public object? Value { get; set; }
    }
}
