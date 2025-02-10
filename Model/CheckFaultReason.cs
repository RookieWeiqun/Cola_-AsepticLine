namespace Cola.Model
{
    public class CheckFaultReason
    {
        public int Id { get; set; }
        public DateTime ConfirmTime { get; set; }
        public DateTime YMDTime { get; set; }
        public DateTime SharpTime { get; set; }
        public string FaultReason { get; set; }
        public int CheckId { get; set; }
        public string CheckName { get; set; }
    }
}
