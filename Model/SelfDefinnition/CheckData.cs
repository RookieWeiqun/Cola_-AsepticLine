using System.Dynamic;

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
    public class CheckDataResult2
    {
        public int Id { get; set; }
        public dynamic Data { get; set; } = new ExpandoObject();
        public int? DeviceId { get; set; }
        public int? LineId { get; set; }
        public string? RecordTime { get; set; }
        public int? RecipeId { get; set; }
        public short? HourCheckValid { get; set; } 
        public string? HourCheckTime { get; set; }
        public int? HourCheckStatus { get; set; }
        public long? AlarmId { get; set; }
    }

    public class CheckDataItem
    {
        public float AsepticTankTopPressure { get; set; }=-1;
        public float CoolingPressureDifference { get; set; }=-1;
        public float CoolingSectionPressure01 { get; set; } = -1;
        public float CoolingSectionPressure02 { get; set; } = -1;
        public float CoolingTemperature { get; set; } = -1;
        public float CrossTemperature { get; set; }= -1;
        public float DegassingTankLiquidLevel { get; set; } = -1;
        public float DegassingTankPressure { get; set; } = -1;
        public float DegassingTankTemperature { get; set; } = -1;
        public float EndTemperature { get; set; } = -1;
        public float HoldingTemperature { get; set; } = -1;
        public float IceWaterPressureDifference { get; set; }=-1;
        public float LiquidLevel { get; set; } = -1;
        public float MixerBottomPressure { get; set; } = -1;
        public int MixerStep { get; set; } = -1;
        public float MixerTopPressure { get; set; } = -1;
        public float ProductFlowRate { get; set; } = -1;
        public float ProductPressure { get; set; } = -1;
        public float ProductTemperature { get; set; } = -1;
        public float RoomTemperature { get; set; } = -1;
        public float SterilizationTime { get; set; } = -1;
        public float Temperature { get; set; } = -1;
        public float TowerWaterPressureDifference { get; set; } = -1;
        public int CleanStatus { get; set; } = -1;
    }
    public class CheckHeadItem
    {

        public string? ProjectDescription { get; set; }

        public string? ReferenceValue { get; set; }

        public string? Unit { get; set; }

        public string? ProjectName { get; set; }

        public string? Keyname { get; set; }

        public int? DeviceId { get; set; }


    }
    public class AlarmItem
    {
        public int? AlarmId { get; set; }
        public string? Valule { get; set; }
        //public int? IsAlarm { get; set; }
        public int? CheckStatus { get; set; }
        public string? CheckUser { get; set; }
        public string? CheckText { get; set; }
    }
    public class CheckWithAlarmData
    {
        public HisDataCheck CheckData { get; set; } = new HisDataCheck();
        public HisDataAlarm? AlarmData { get; set; } = new HisDataAlarm();
        public DateTime? SharpTime { get; set; }
    }
}
