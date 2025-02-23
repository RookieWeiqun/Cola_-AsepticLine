using Cola.Model;
using Cola.Service.IService;

namespace Cola.Service
{
    public class ReportService : IReportService
    {
        public Task<List<RealtimeDataResult>> BuildRealtimeDataResults(IEnumerable<RealtimeData> realtimeDatas, IDictionary<int, string> deviceTypeList, IDictionary<int, string> deviceStepList, IDictionary<int, CheckPara> checkParas, IEnumerable<DeviceState> deviceStateList)
        {
            throw new NotImplementedException();
        }
    }
}
