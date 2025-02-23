using Cola.Model;

namespace Cola.Service.IService
{
    public interface IReportService
    {
      Task<List<RealtimeDataResult>> BuildRealtimeDataResults(
      IEnumerable<RealtimeData> realtimeDatas,
      IDictionary<int, string> deviceTypeList,
      IDictionary<int, string> deviceStepList,
      IDictionary<int, CheckPara> checkParas,
      IEnumerable<DeviceState> deviceStateList);
    }
}
