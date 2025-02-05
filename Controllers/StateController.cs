using AutoMapper;
using Cola.Model;
using Cola.Model.SelfDefinnition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Cola.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //用于甘特图状态的控制器
    public class StateController:ControllerBase
    {
        private readonly ILogger<StateController> _logger;
        private readonly IFreeSql _fsql;
        private readonly IMapper _mapper;

        public StateController(ILogger<StateController> logger, IFreeSql fsql, IMapper mapper)
        {
            _logger = logger;
            _fsql = fsql;
            _mapper = mapper;
        }
        [HttpGet(Name = "GetState")]
        public async Task<IActionResult> GetReportDataByInputTime(int deviceId)
        {
            try
            {
                _logger.LogInformation("开始获取检查参数数据");


                // 1. 查询最近时间的RealtimeData并加载DeviceInfo
                var realtimeDatas = await _fsql.Select<HisDataState>()
                       .Where(r => r.DeviceId == deviceId)
                       //.Include(r => r.DeviceInfo)
                       .ToListAsync();

                _logger.LogInformation("成功获取检查参数数据，数量：{Count}", realtimeDatas.Count);

                // 2. 收集所有CheckPara的ID
                var allCheckParaIds = realtimeDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 3. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 4. 构建结果
                var results = new List<StateDataResult>();

                foreach (var realtimeData in realtimeDatas)
                {
                    var resultItem = new StateDataResult
                    {
                        Id =realtimeData.Id,
                        DeviceId = realtimeData.DeviceId,
                        LineId = realtimeData.LineId,
                        StateId = realtimeData.StateId,
                        BeginTime = realtimeData.BeginTime,
                        EndTime = realtimeData.EndTime
                    };

                    if (realtimeData.Data != null)
                    {
                        var dataDict = (JObject)realtimeData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                resultItem.Data.Add(new StateDataItem
                                {
                                    //RealtimeDataId = realtimeData.Id,
                                    CheckParaId = checkParaId,
                                    CheckParaAliasName = checkPara.AliasName,
                                    Value = prop.Value.ToObject<object>()
                                });
                            }
                        }
                    }

                    results.Add(resultItem);
                }

                // 返回标准响应格式
                return Ok(realtimeDatas);
                //return Ok(new ApiResponse<IEnumerable<HisDataCheck>>(200, data, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检查参数数据时发生错误");

                // 返回错误响应格式
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误"));
            }
        }
    }
}
