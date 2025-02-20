using AutoMapper;
using Cola.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Cola.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //用于报表的控制器
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IFreeSql _fsql;
        private readonly IMapper _mapper;

        public ReportController(ILogger<ReportController> logger, IFreeSql fsql, IMapper mapper)
        {
            _logger = logger;
            _fsql = fsql;
            _mapper = mapper;
        }
        [HttpGet("history", Name = "GetHistoryTimeReport")]
        public async Task<IActionResult> GetReportDataByInputTime([FromQuery] DateTime? inputTime)
        {
            if (!inputTime.HasValue)
            {
                return BadRequest(new ApiResponse<object>(400, null, "输入时间不能为空"));
            }
            try
            {
                _logger.LogInformation("开始获取检查参数数据");

                // 0. 找到离当前时间最近的 update_time
                var closestUpdateTime = await FindClosestUpdateTime(inputTime);
                if(!closestUpdateTime.HasValue)
                {
                    return NotFound(new ApiResponse<object>(404, null, "未找到数据"));
                }
                // 将本地时间转换为 UTC 时间
                //if (closestUpdateTime.HasValue)
                //{
                //    latestUpdateTime = latestUpdateTime.Value.ToUniversalTime();
                //    closestUpdateTime = closestUpdateTime.Value.ToUniversalTime();
                //}
                _logger.LogInformation("Latest Update Time: {closestUpdateTime}", closestUpdateTime);
                // 1. 查询最近时间的RealtimeData并加载DeviceInfo
                var realtimeDatas = await _fsql.Select<HisDataCheck>()
                       .Where(r => r.RecordTime.Value.Ticks == closestUpdateTime.Value.Ticks)
                       .Include(r => r.DeviceInfo)
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
                var results = new List<HistoryDataResult>();

                foreach (var realtimeData in realtimeDatas)
                {
                    var resultItem = new HistoryDataResult
                    {
                        Id = realtimeData.Id,
                        DeviceId = realtimeData.DeviceId,
                        LineId = realtimeData.LineId,
                        RecipeId = realtimeData.RecipeId,
                        RecordTime = realtimeData.RecordTime,
                        DeviceInfo = realtimeData.DeviceInfo
                    };

                    if (realtimeData.Data != null)
                    {
                        var dataDict = (JObject)realtimeData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                resultItem.Data.Add(new DataItem
                                {
                                    RealtimeDataId = realtimeData.Id,
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
                //return Ok(results);
                return Ok(new ApiResponse<IEnumerable<HistoryDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检查参数数据时发生错误");

                // 返回错误响应格式
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误"));
            }
        }
        [HttpGet("current", Name = "GetCurrentTimeReport")]
        public async Task<IActionResult> GetCurrentTimeReport()
        {
            try
            {
                _logger.LogInformation("开始获取检查参数数据");
                // 1. 查询当前时间的RealtimeData并加载DeviceInfo
                var realtimeDatas = await _fsql.Select<RealtimeData>()
                       .Include(r => r.DeviceInfo)
                       .ToListAsync();
                if (realtimeDatas.Count == 0)
                {
                    return NotFound(new ApiResponse<object>(404, null, "未找到数据"));
                }
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
                var results = new List<RealtimeDataResult>();

                foreach (var realtimeData in realtimeDatas)
                {
                    var resultItem = new RealtimeDataResult
                    {
                        Id = realtimeData.Id,
                        DeviceId = realtimeData.DeviceId,
                        LineId = realtimeData.LineId,
                        RecipeId = realtimeData.RecipeId,
                        UpdateTime = realtimeData.UpdateTime,
                        DeviceInfo = realtimeData.DeviceInfo
                    };

                    if (realtimeData.Data != null)
                    {
                        var dataDict = (JObject)realtimeData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                resultItem.Data.Add(new DataItem
                                {
                                    RealtimeDataId = realtimeData.Id,
                                    CheckParaId = checkParaId,
                                    CheckParaAliasName = checkPara.AliasName,
                                    Value = prop.Value.ToObject<object>()
                                });
                            }
                        }
                    }

                    results.Add(resultItem);
                }
                return Ok(new ApiResponse<IEnumerable<RealtimeDataResult>>(200, results, "成功"));
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取检查参数数据时发生错误");

                // 返回错误响应格式
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误"));
            }
        }
        // 辅助方法：找到离输入时间最近的 update_time
        private async Task<DateTime?> FindClosestUpdateTime(DateTime? inputTime)
        {
            var inputTimeToRes = inputTime;
            // 找到比输入时间早的最大的时间
            var earlierTime = await _fsql.Select<HisDataCheck>()
                .Where(r => r.RecordTime <= inputTimeToRes)
                .OrderByDescending(r => r.RecordTime)
                .FirstAsync(r => r.RecordTime);

            // 找到比输入时间晚的最早的时间
            var laterTime = await _fsql.Select<HisDataCheck>()
                .Where(r => r.RecordTime >= inputTimeToRes)
                .OrderBy(r => r.RecordTime)
                .FirstAsync(r => r.RecordTime);

            // 比较两者，选择更接近的时间
            if (earlierTime == null && laterTime == null) return null;
            if (earlierTime == null) return laterTime;
            if (laterTime == null) return earlierTime;

            var timeDiffEarlier = inputTime - earlierTime.Value;
            var timeDiffLater = laterTime.Value - inputTime;

            return timeDiffEarlier < timeDiffLater ? earlierTime : laterTime;
        }
    }
}
