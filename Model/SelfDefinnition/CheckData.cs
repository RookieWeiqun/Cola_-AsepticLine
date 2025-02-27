namespace Cola.Model
{
    public class CheckDataResult
    {
        public int Id { get; set; }
        public CheckDataItem Data { get; set; } = new CheckDataItem();
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        public string? RecordTime { get; set; }
        public int? RecipeId { get; set; }
    }

    public class CheckDataItem
    {
        public float AsepticTankTopPressure { get; set; }
        public float CoolingPressureDifference { get; set; }
        public float CoolingSectionPressure01 { get; set; }
        public float CoolingSectionPressure02 { get; set; }
        public float CoolingTemperature { get; set; }
        public float CrossTemperature { get; set; }
        public float DegassingTankLiquidLevel { get; set; }
        public float DegassingTankPressure { get; set; }
        public float DegassingTankTemperature { get; set; }
        public float EndTemperature { get; set; }
        public float HoldingTemperature { get; set; }
        public float IceWaterPressureDifference { get; set; }
        public float LiquidLevel { get; set; }
        public float MixerBottomPressure { get; set; }
        public int MixerStep { get; set; }
        public float MixerTopPressure { get; set; }
        public float ProductFlowRate { get; set; }
        public float ProductPressure { get; set; }
        public float ProductTemperature { get; set; }
        public float RoomTemperature { get; set; }
        public float SterilizationTime { get; set; }
        public float Temperature { get; set; }
        public float TowerWaterPressureDifference { get; set; }
    }
}
