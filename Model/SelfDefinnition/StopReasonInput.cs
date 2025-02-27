namespace Cola.Model
{
    public class StopReasonInput
    {
        public int Id { get; set; }
        public int? ReasonId { get; set; } // Make ReasonId nullable
        public string? StopDef { get; set; } // Add StopDef property
    }
}
