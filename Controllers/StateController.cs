using AutoMapper;
using Cola.Model;
using Cola.Model.SelfDefinnition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Cola.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //用于甘特图状态的控制器
    public class StateController : ControllerBase
    {
        private readonly ILogger<StateController> _logger;
        private readonly IFreeSql _fsql;
        private readonly IMapper _mapper;
        private readonly AppConfig _appConfig;

        public StateController(ILogger<StateController> logger, IFreeSql fsql, IMapper mapper, IOptions<AppConfig> options)
        {
            _logger = logger;
            _fsql = fsql;
            _mapper = mapper;
            _appConfig = options.Value; // 获取配置实例
        }
        [HttpGet(Name = "获取当前时间的一个完整产程的甘特图数据")]
        public async Task<IActionResult> GetReportDataByInputTime([FromQuery] int deviceId, [FromQuery] DateTime? inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 的检查参数数据,CIP为 {CIP}", deviceId, _appConfig.CIP);

                // 1. 参数校验
                if (!inputTime.HasValue)
                {
                    return StatusCode(400, new ApiResponse<object>(400, null, "必须提供 inputTime 参数"));
                }

                // 2. 找到离 inputTime 最近的 CIP 的 BeginTime（降序取第一条）
                var cutoffTime = await _fsql.Select<HisDataState>()
                    .Where(s =>
                        s.DeviceId == deviceId &&
                        s.StateId == _appConfig.CIP &&
                        s.BeginTime <= inputTime)
                    .OrderByDescending(s => s.BeginTime) // 关键优化点：降序排序
                    .FirstAsync(s => s.BeginTime);
                if (!cutoffTime.HasValue)
                {
                    return StatusCode(404, new ApiResponse<object>(404, null, "未找到数据"));
                }
                List<HisDataState> stateDatas = new List<HisDataState>();
                // 3. 如果没有找到 StateId=666 的记录，直接返回inputTime前的所有数据
                if (!cutoffTime.HasValue)
                {
                    stateDatas = await _fsql.Select<HisDataState>()
                    .Where(s =>
                        s.DeviceId == deviceId &&
                        s.BeginTime <= inputTime)
                    .OrderBy(s => s.BeginTime)
                    .ToListAsync();
                }
                // 4. 查询 cutoffTime 到 inputTime 之间的数据
                stateDatas = await _fsql.Select<HisDataState>()
                   .Where(s =>
                       s.DeviceId == deviceId &&
                       s.BeginTime >= cutoffTime &&
                       s.BeginTime <= inputTime)
                   .OrderBy(s => s.BeginTime)
                   .ToListAsync();
                // 5. 收集所有CheckPara的ID
                var allCheckParaIds = stateDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 6. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 7. 构建结果
                var results = new List<StateDataResult>();

                foreach (var stateData in stateDatas)
                {
                    var resultItem = new StateDataResult
                    {
                        Id = stateData.Id,
                        DeviceId = stateData.DeviceId,
                        LineId = stateData.LineId,
                        RecordTime = stateData.RecordTime,
                        BeginTime = stateData.BeginTime,
                        Duration = stateData.Duration,
                        EndTime = stateData.EndTime,
                        StateId = stateData.StateId,
                    };

                    if (stateData.Data != null)
                    {
                        var dataDict = (JObject)stateData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                resultItem.Data.Add(new StateDataItem
                                {
                                    StateDataId = stateData.Id,
                                    CheckParaId = checkParaId,
                                    CheckParaAliasName = checkPara.AliasName,
                                    Value = prop.Value.ToObject<object>()
                                });
                            }
                        }
                    }

                    results.Add(resultItem);
                }
                return Ok(new ApiResponse<IEnumerable<StateDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败", deviceId);
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误")); ;
            }
        }
    }
}
