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
                _logger.LogInformation("Latest Update Time: {closestUpdateTime}", closestUpdateTime);
                // 1. 查询最近时间的RealtimeData并加载DeviceInfo
                var historytimeDatas = await _fsql.Select<HisDataCheck>()
                       .Where(r => r.RecordTime.Value.Ticks == closestUpdateTime.Value.Ticks)
                       .Include(r => r.DeviceInfo)
                       .ToListAsync();

                _logger.LogInformation("成功获取检查参数数据，数量：{Count}", historytimeDatas.Count);

                // 2. 收集所有CheckPara的ID
                var allCheckParaIds = historytimeDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 3. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 4. 构建结果
                // 4.1 获取状态列表
                var deviceStateList = await _fsql.Select<DeviceState>()
                       .ToListAsync();
                // 4.2 获取Device_type列表
                var deviceTypeList = (await _fsql.Select<DeviceType>()
                 .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                var results = new List<RealtimeDataResult>();
                // 4.3 获取device_step列表
                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 4.4 获取recipe_info列表
                var recipeInfoList = (await _fsql.Select<RecipeInfo>()
               .ToListAsync()).ToDictionary(dt => dt.Sku, dt => dt.Name);
                // 4.5 构建结果
                foreach (var historyimeData in historytimeDatas)
                {
                    var resultItem = new RealtimeDataResult
                    {
                        Id = historyimeData.Id,
                        DeviceId = historyimeData.DeviceId,
                        LineId = historyimeData.LineId,
                        RecipeId = historyimeData.RecipeId,
                        RecordTime = historyimeData.RecordTime,
                        Name = deviceTypeList.TryGetValue(historyimeData.DeviceInfo.DeviceType ?? 0, out var deviceTypeName) ? deviceTypeName : null,
                    };

                    if (historyimeData.Data != null)
                    {
                        var dataDict = (JObject)historyimeData.Data;
                        var reportDataItem = new ReportDataItem();
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to ReportDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.Weight:
                                        reportDataItem.Weight = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CleanStatus:
                                        //先暂时返回0，等数据库建好后返回具体值
                                        //var statusValue = prop.Value.ToObject<int>();
                                        //var deviceState = deviceStateList.FirstOrDefault(ds => ds.Id == statusValue);
                                        //if (deviceState != null)
                                        //{
                                        //    reportDataItem.CleanStatus = deviceState.Name;
                                        //}
                                        reportDataItem.CleanStatus = prop.Value.ToObject<int>().ToString();
                                        break;
                                    case CheckPara_KeyName.BlendStatus:
                                        //var statusValue = prop.Value.ToObject<int>();
                                        //var deviceState = deviceStateList.FirstOrDefault(ds => ds.Id == statusValue);
                                        //if (deviceState != null)
                                        //{
                                        //    reportDataItem.BlendStatus = deviceState.Name;
                                        //}
                                        reportDataItem.BlendStatus = prop.Value.ToObject<int>().ToString();
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        reportDataItem.ProductFlowRate = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.Name:
                                        reportDataItem.Name = prop.Value.ToObject<string>();
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        var mixerStepId = prop.Value.ToObject<int>();
                                        if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                        {
                                            reportDataItem.MixerStep = mixerStepName;
                                        }
                                        break;
                                    case CheckPara_KeyName.Temperature:
                                        reportDataItem.Temperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.LiquidLevel:
                                        reportDataItem.LiquidLevel = prop.Value.ToObject<float>();
                                        break;
                                }
                            }
                            reportDataItem.Name = historyimeData.DeviceInfo.Name;
                            if (recipeInfoList.TryGetValue(historyimeData.RecipeId.ToString(), out var recipeName))
                            {
                                reportDataItem.Formula = recipeName;
                            }
                            reportDataItem.Capacity = historyimeData.DeviceInfo.Capacity == 0 ? 0 : historyimeData.DeviceInfo.Capacity;
                            //设备状态这里先不写！！！！等温工
                            //var deviceState = deviceStateList.FirstOrDefault(ds => ds.Value == historyimeData.StateId);
                            //if (deviceState != null)
                            //{
                            //    reportDataItem.DeviceStatus = deviceState.Name;
                            //}
                        }
                        resultItem.Data = reportDataItem;
                    }
                    results.Add(resultItem);
                }

                // 返回标准响应格式
                //return Ok(results);
                return Ok(new ApiResponse<IEnumerable<RealtimeDataResult>>(200, results, "成功"));
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
                // 4.1 获取状态列表
                var deviceStateList =await _fsql.Select<DeviceState>()
                       .ToListAsync();
                // 4.2 获取Device_type列表
                var deviceTypeList = (await _fsql.Select<DeviceType>()
                 .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                var results = new List<RealtimeDataResult>();
                // 4.3 获取device_step列表
                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt=>dt.Id, dt=>dt.Name);
                // 4.4 获取recipe_info列表
                var recipeInfoList  = (await _fsql.Select<RecipeInfo>()
               .ToListAsync()).ToDictionary(dt => dt.Sku, dt => dt.Name);
                // 4.4 构建结果
                foreach (var realtimeData in realtimeDatas)
                {
                    var resultItem = new RealtimeDataResult
                    {
                        Id = realtimeData.Id,
                        DeviceId = realtimeData.DeviceId,
                        LineId = realtimeData.LineId,
                        RecipeId = realtimeData.RecipeId,
                        RecordTime = realtimeData.UpdateTime,
                        Name = deviceTypeList.TryGetValue(realtimeData.DeviceInfo.DeviceType ?? 0, out var deviceTypeName) ? deviceTypeName : null,
                    };

                    if (realtimeData.Data != null)
                    {
                        var dataDict = (JObject)realtimeData.Data;
                        var reportDataItem = new ReportDataItem();
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to ReportDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.Weight:
                                        reportDataItem.Weight = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CleanStatus:
                                        //先暂时返回0，等数据库建好后返回具体值
                                        //var statusValue = prop.Value.ToObject<int>();
                                        //var deviceState = deviceStateList.FirstOrDefault(ds => ds.Id == statusValue);
                                        //if (deviceState != null)
                                        //{
                                        //    reportDataItem.CleanStatus = deviceState.Name;
                                        //}
                                        reportDataItem.CleanStatus = prop.Value.ToObject<int>().ToString();
                                        break;
                                    case CheckPara_KeyName.BlendStatus:
                                        //var statusValue = prop.Value.ToObject<int>();
                                        //var deviceState = deviceStateList.FirstOrDefault(ds => ds.Id == statusValue);
                                        //if (deviceState != null)
                                        //{
                                        //    reportDataItem.BlendStatus = deviceState.Name;
                                        //}
                                        reportDataItem.BlendStatus = prop.Value.ToObject<int>().ToString();
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        reportDataItem.ProductFlowRate = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.Name:
                                        reportDataItem.Name = prop.Value.ToObject<string>();
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        var mixerStepId = prop.Value.ToObject<int>();
                                        if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                        {
                                            reportDataItem.MixerStep = mixerStepName;
                                        }
                                        break;
                                    case CheckPara_KeyName.Temperature:
                                        reportDataItem.Temperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.LiquidLevel:
                                        reportDataItem.LiquidLevel = prop.Value.ToObject<float>();
                                        break;
                                }
                            }
                            reportDataItem.Name= realtimeData.DeviceInfo.Name;
                            if(recipeInfoList.TryGetValue(realtimeData.RecipeId.ToString(), out var recipeName))
                            {
                                reportDataItem.Formula = recipeName;
                            }
                            var deviceState = deviceStateList.FirstOrDefault(ds => ds.Value == realtimeData.StateId);
                            if (deviceState != null)
                            {
                                reportDataItem.DeviceStatus = deviceState.Name;
                            }
                            reportDataItem.Capacity = realtimeData.DeviceInfo.Capacity == 0 ?   0:realtimeData.DeviceInfo.Capacity;
                        }
                        resultItem.Data = reportDataItem;
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
