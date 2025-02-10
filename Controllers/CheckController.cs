using AutoMapper;
using Cola.DTO;
using Cola.Extensions;
using Cola.Model;
using Cola.Model.SelfDefinnition;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Cola.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckController : ControllerBase
    {
        private readonly ILogger<CheckController> _logger;
        private readonly IFreeSql _fsql;
        private readonly IMapper _mapper;

        public CheckController(ILogger<CheckController> logger, IFreeSql fsql, IMapper mapper)
        {
            _logger = logger;
            _fsql = fsql;
            _mapper = mapper;
        }

        [HttpGet("currunt", Name = "获取当前时间点检数据")]
        public async Task<IActionResult> GetCurrentTimeCheckData([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 在 {InputTime} 的检查数据", deviceId, inputTime);

                // ================== 第一部分：获取当前时间的记录 ==================
                var closestRecord = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime <= inputTime)
                    .Include(c => c.DeviceInfo) // 加载设备信息
                    .OrderByDescending(c => c.RecordTime)
                    .FirstAsync();


                // ================== 第二部分：处理数据转换 ==================
                // 1. 收集所有CheckPara的ID
                var allCheckParaIds = ((JObject)closestRecord.Data)
                    .Properties()
                    .Select(p => int.Parse(p.Name))
                    .Distinct()
                    .ToList();
                // 2. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 3. 构建结果
                var resultItem = new CheckDataResult
                {
                    Id = closestRecord.Id,
                    DeviceId = closestRecord.DeviceId,
                    LineId = closestRecord.LineId,
                    RecipeId = closestRecord.RecipeId,
                    RecordTime = closestRecord.RecordTime,
                };
                if (closestRecord.Data != null)
                {
                    var dataDict = (JObject)closestRecord.Data;
                    foreach (var prop in dataDict.Properties())
                    {
                        var checkParaId = int.Parse(prop.Name);
                        if (checkParas.TryGetValue(checkParaId, out var checkPara))
                        {
                            resultItem.Data.Add(new CheckDataItem
                            {
                                CheckDataId = closestRecord.Id,
                                CheckParaId = checkParaId,
                                CheckParaAliasName = checkPara.AliasName,
                                Value = prop.Value.ToObject<object>()
                            });
                        }
                    }
                }

                return Ok(new ApiResponse<CheckDataResult>(200, resultItem, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败 | 输入时间：{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));
            }
        }
        [HttpGet("sharp", Name = "获取整点时间点检数据")]
        public async Task<IActionResult> GetSharpTimeCheckData([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                // ================== 第一部分：获取当日整点数据 ==================
                // 1. 生成当日所有整点时间（00:00, 01:00,...,23:00）
                var dayStart = inputTime.Date;
                var hourlyPoints = Enumerable.Range(0, 24)
                    .Select(h => dayStart.AddHours(h))
                    .ToList();

                // 2. 查询当天所有数据
                var dayEnd = dayStart.AddDays(1);
                var allRecords = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime >= dayStart &&
                        c.RecordTime < dayEnd)
                    .ToListAsync();

                // 3. 在本地处理数据，找到每个整点附近的数据（时间窗口 ±1 分钟）
                var hourlyDatas = new List<HisDataCheck>();
                foreach (var hour in hourlyPoints)
                {
                    var start = hour.AddSeconds(-60);
                    var end = hour.AddSeconds(60);

                    var recordsInWindow = allRecords
                        .Where(c => c.RecordTime >= start && c.RecordTime <= end)
                        .OrderBy(c => Math.Abs((c.RecordTime - hour).Value.Ticks))
                        .FirstOrDefault();

                    if (recordsInWindow != null)
                    {
                        hourlyDatas.Add(recordsInWindow);
                    }
                }
                // ================== 第二部分：处理数据转换 ==================
                // 2. 收集所有CheckPara的ID
                var allCheckParaIds = hourlyDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 3. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 4. 构建结果
                var results = new List<CheckDataResult>();

                foreach (var hourlyData in hourlyDatas)
                {
                    var resultItem = new CheckDataResult
                    {
                        Id = hourlyData.Id,
                        DeviceId = hourlyData.DeviceId,
                        LineId = hourlyData.LineId,
                        RecipeId = hourlyData.RecipeId,
                        RecordTime = hourlyData.RecordTime,
                    };

                    if (hourlyData.Data != null)
                    {
                        var dataDict = (JObject)hourlyData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                resultItem.Data.Add(new CheckDataItem
                                {
                                    CheckDataId = hourlyData.Id,
                                    CheckParaId = checkParaId,
                                    CheckParaAliasName = checkPara.AliasName,
                                    Value = prop.Value.ToObject<object>()
                                });
                            }
                        }
                    }

                    results.Add(resultItem);
                }
                return Ok(new ApiResponse<IEnumerable<CheckDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败 | 输入时间：{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));

            }
        }
    }
}
