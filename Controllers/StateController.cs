using AutoMapper;
using Cola.Model;
using Cola.Model.SelfDefinnition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

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
        public async Task<IActionResult> GetReportDataByInputTime([FromQuery, Required] int deviceId, [FromQuery, Required] DateTime? inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 的检查参数数据,CIP为 {CIP}", deviceId, _appConfig.CIP);

                // 1. 参数校验
                if (!inputTime.HasValue)
                {
                    return StatusCode(400, new ApiResponse<object>(200, null, "必须提供 inputTime 参数"));
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
                    return StatusCode(200, new ApiResponse<object>(200, null, "未找到数据"));
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
                    .Include(r => r.DeviceInfo)
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
                // 7.1 获取状态列表
                var deviceStateList = await _fsql.Select<DeviceState>()
                       .ToListAsync();
                var blendStateList = await _fsql.Select<BlendState>()
                        .ToListAsync();
                // 7.2 获取Device_type列表
                var deviceTypeList = (await _fsql.Select<DeviceType>()
                 .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 7.3 获取device_step列表
                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 7.4 获取停机原因列表
                var stopReasonList = (await _fsql.Select<StopState>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 7.5 获取recipe_info列表
                var recipeInfoList = (await _fsql.Select<RecipeInfo>()
               .ToListAsync()).ToDictionary(dt => dt.Sku, dt => dt.Name);
                var results = new List<StateDataResult>();
                foreach (var stateData in stateDatas)
                {
                    var stateDataResult = new StateDataResult
                    {
                        Id = stateData.Id,
                        DeviceId = stateData.DeviceId,
                        LineId = stateData.LineId,
                        //RecordTime = stateData.RecordTime,
                        BeginTime = stateData.BeginTime,
                        Duration = stateData.Duration,
                        EndTime = stateData.EndTime,
                        DeviceStatus = deviceStateList.FirstOrDefault(ds => ds.Value == stateData.StateId)?.Name,
                        Formula = recipeInfoList.TryGetValue(stateData.RecipeId.ToString(), out var recipeName) ? recipeName : null,
                        StopReason = stateData.StopId.HasValue && stopReasonList.TryGetValue(stateData.StopId.Value, out var stopReason) ? stopReason : stateData.StopDef,
                        Capacity = stateData.DeviceInfo.Capacity == 0 ? 0 : stateData.DeviceInfo.Capacity
                    };

                    if (stateData.Data != null)
                    {
                        var dataDict = (JObject)stateData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to ReportDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.Weight:
                                        stateDataResult.Weight = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.BlendStatus:
                                        var statusValue = prop.Value.ToObject<int>();
                                        var deviceState = blendStateList.FirstOrDefault(ds => ds.Value == statusValue);
                                        if (deviceState != null)
                                        {
                                            stateDataResult.BlendStatus = deviceState.Name;
                                        }
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        stateDataResult.ProductFlowRate = prop.Value.ToObject<int>();
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        var mixerStepId = prop.Value.ToObject<int>();
                                        if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                        {
                                            stateDataResult.MixerStep = mixerStepName;
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    results.Add(stateDataResult);
                }
                return Ok(new ApiResponse<IEnumerable<StateDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败", deviceId);
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误")); ;
            }
        }
        [HttpGet("last-n-hours", Name = "GetReportDataByLastNHours")]
        public async Task<IActionResult> GetReportDataByLastNHours([FromQuery, Required] int deviceId, [FromQuery, Required] DateTime? inputTime, [FromQuery, Required] int hours)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 的检查参数数据，最近 {Hours} 小时", deviceId, hours);

                // 1. 参数校验
                if (!inputTime.HasValue)
                {
                    return StatusCode(400, new ApiResponse<object>(400, null, "必须提供 inputTime 参数"));
                }
                if (hours <= 0)
                {
                    return StatusCode(400, new ApiResponse<object>(400, null, "必须提供有效的小时数"));
                }

                var endTime = inputTime.Value;
                var startTime = endTime.AddHours(-hours);

                // 2. 查询 startTime 到 endTime 之间的数据
                var stateDatas = await _fsql.Select<HisDataState>()
                   .Where(s =>
                       s.DeviceId == deviceId &&
                       s.EndTime >= startTime &&
                       s.BeginTime <= endTime)
                   .OrderBy(s => s.BeginTime)
                   .Include(r => r.DeviceInfo)
                   .ToListAsync();

                // 3. 收集所有CheckPara的ID
                var allCheckParaIds = stateDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 4. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 5. 构建结果
                // 5.1 获取状态列表
                var deviceStateList = await _fsql.Select<DeviceState>()
                       .ToListAsync();
                // 5.2 获取Device_type列表
                var deviceTypeList = (await _fsql.Select<DeviceType>()
                 .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                var blendStateList = await _fsql.Select<BlendState>()
                 .ToListAsync();
                // 5.3 获取device_step列表
                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 5.4 获取停机原因列表
                var stopReasonList = (await _fsql.Select<StopState>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 5.5 获取recipe_info列表
                var recipeInfoList = (await _fsql.Select<RecipeInfo>()
               .ToListAsync()).ToDictionary(dt => dt.Sku, dt => dt.Name);
                var results = new List<StateDataResult>();
                foreach (var stateData in stateDatas)
                {
                    var stateDataResult = new StateDataResult
                    {
                        Id = stateData.Id,
                        DeviceId = stateData.DeviceId,
                        LineId = stateData.LineId,
                        //RecordTime = stateData.RecordTime,
                        BeginTime = stateData.BeginTime,
                        Duration = stateData.Duration,
                        EndTime = stateData.EndTime,
                        DeviceStatus = deviceStateList.FirstOrDefault(ds => ds.Value == stateData.StateId)?.Name,
                        Formula = recipeInfoList.TryGetValue(stateData.RecipeId.ToString(), out var recipeName) ? recipeName : null,
                        StopReason = stateData.StopId.HasValue && stopReasonList.TryGetValue(stateData.StopId.Value, out var stopReason) ? stopReason : stateData.StopDef,
                        Capacity = stateData.DeviceInfo.Capacity == 0 ? 0 : stateData.DeviceInfo.Capacity
                    };

                    if (stateData.Data != null)
                    {
                        var dataDict = (JObject)stateData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to ReportDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.Weight:
                                        stateDataResult.Weight = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        stateDataResult.ProductFlowRate = prop.Value.ToObject<int>();
                                        break;
                                    case CheckPara_KeyName.BlendStatus:
                                        var statusValue = prop.Value.ToObject<int>();
                                        var deviceState = blendStateList.FirstOrDefault(ds => ds.Value == statusValue);
                                        if (deviceState != null)
                                        {
                                            stateDataResult.BlendStatus = deviceState.Name;
                                        }
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        var mixerStepId = prop.Value.ToObject<int>();
                                        if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                        {
                                            stateDataResult.MixerStep = mixerStepName;
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    results.Add(stateDataResult);
                }
                return Ok(new ApiResponse<IEnumerable<StateDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败", deviceId);
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误")); ;
            }
        }

        [HttpGet("current-shift", Name = "GetReportDataByCurrentShift")]
        public async Task<IActionResult> GetReportDataByCurrentShift([FromQuery, Required] int deviceId, [FromQuery, Required] DateTime? inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 的检查参数数据，当前班次", deviceId);

                // 1. 参数校验
                if (!inputTime.HasValue)
                {
                    return StatusCode(400, new ApiResponse<object>(400, null, "必须提供 inputTime 参数"));
                }

                DateTime shiftStartTime;
                DateTime shiftEndTime;

                if (inputTime.Value.Hour >= 7 && inputTime.Value.Hour < 19)
                {
                    // Day shift: 7:00 to 19:00
                    shiftStartTime = inputTime.Value.Date.AddHours(7);
                    shiftEndTime = inputTime.Value.Date.AddHours(19);
                }
                else
                {
                    // Night shift: 19:00 to 7:00
                    if (inputTime.Value.Hour >= 19)
                    {
                        shiftStartTime = inputTime.Value.Date.AddHours(19);
                        shiftEndTime = inputTime.Value.Date.AddDays(1).AddHours(7);
                    }
                    else
                    {
                        shiftStartTime = inputTime.Value.Date.AddDays(-1).AddHours(19);
                        shiftEndTime = inputTime.Value.Date.AddHours(7);
                    }
                }

                // 2. 查询 shiftStartTime 到 inputTime 之间的数据
                var stateDatas = await _fsql.Select<HisDataState>()
                   .Where(s =>
                       s.DeviceId == deviceId &&
                       s.EndTime >= shiftStartTime ||
                       s.BeginTime <= inputTime)
                   .OrderBy(s => s.BeginTime)
                   .Include(r => r.DeviceInfo)
                   .ToListAsync();

                // 3. 收集所有CheckPara的ID
                var allCheckParaIds = stateDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 4. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 5. 构建结果
                // 5.1 获取状态列表
                var deviceStateList = await _fsql.Select<DeviceState>()
                       .ToListAsync();
                var blendStateList = await _fsql.Select<BlendState>()
                        .ToListAsync();
                // 5.2 获取Device_type列表
                var deviceTypeList = (await _fsql.Select<DeviceType>()
                 .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 5.3 获取device_step列表
                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 5.4 获取停机原因列表
                var stopReasonList = (await _fsql.Select<StopState>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
                // 5.5 获取recipe_info列表
                var recipeInfoList = (await _fsql.Select<RecipeInfo>()
               .ToListAsync()).ToDictionary(dt => dt.Sku, dt => dt.Name);
                var results = new List<StateDataResult>();
                foreach (var stateData in stateDatas)
                {
                    var stateDataResult = new StateDataResult
                    {
                        Id = stateData.Id,
                        DeviceId = stateData.DeviceId,
                        LineId = stateData.LineId,
                        //RecordTime = stateData.RecordTime,
                        BeginTime = stateData.BeginTime,
                        Duration = stateData.Duration,
                        EndTime = stateData.EndTime,
                        DeviceStatus = deviceStateList.FirstOrDefault(ds => ds.Value == stateData.StateId)?.Name,
                        Formula = recipeInfoList.TryGetValue(stateData.RecipeId.ToString(), out var recipeName) ? recipeName : null,
                        StopReason = stateData.StopId.HasValue && stopReasonList.TryGetValue(stateData.StopId.Value, out var stopReason) ? stopReason : stateData.StopDef,
                        Capacity = stateData.DeviceInfo.Capacity == 0 ? 0 : stateData.DeviceInfo.Capacity
                    };

                    if (stateData.Data != null)
                    {
                        var dataDict = (JObject)stateData.Data;
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to ReportDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.Weight:
                                        stateDataResult.Weight = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        stateDataResult.ProductFlowRate = prop.Value.ToObject<int>();
                                        break;
                                    case CheckPara_KeyName.BlendStatus:
                                        var statusValue = prop.Value.ToObject<int>();
                                        var deviceState = blendStateList.FirstOrDefault(ds => ds.Value == statusValue);
                                        if (deviceState != null)
                                        {
                                            stateDataResult.BlendStatus = deviceState.Name;
                                        }
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        var mixerStepId = prop.Value.ToObject<int>();
                                        if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                        {
                                            stateDataResult.MixerStep = mixerStepName;
                                        }
                                        break;
                                }
                            }
                        }
                    }

                    results.Add(stateDataResult);
                }
                return Ok(new ApiResponse<IEnumerable<StateDataResult>>(200, results, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceId} 数据失败", deviceId);
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误")); ;
            }
        }
        //用与写入设备停机原因
        [HttpPost("stop-reason", Name = "写入设备停机原因")]
        public async Task<IActionResult> WriteStopReason([FromBody] StopReasonInput input)
        {
            try
            {
                _logger.LogInformation("开始写入设备 {DeviceId} 的停机原因", input.Id);

                // 1. 参数校验
                if (input.ReasonId == null && string.IsNullOrEmpty(input.StopDef))
                {
                    return StatusCode(400, new ApiResponse<object>(200, null, "必须提供有效的 ReasonId 或 StopDef"));
                }

                // 2. 查询最近的一条未结束的数据
                var stateData = await _fsql.Select<HisDataState>()
                    .Where(s => s.Id == input.Id)
                    .FirstAsync();
                if (stateData == null)
                {
                    return StatusCode(200, new ApiResponse<object>(400, null, "未找到对应的数据"));
                }

                // 3. 更新数据
                if (input.ReasonId.HasValue)
                {
                    stateData.StopId = input.ReasonId.Value;
                    await _fsql.Update<HisDataState>()
                        .Set(s => s.StopId, input.ReasonId.Value)
                        .Set(s => s.StopDef, (string)null)
                        .Where(s => s.Id == stateData.Id)
                        .ExecuteAffrowsAsync();
                }
                else if (!string.IsNullOrEmpty(input.StopDef))
                {
                    stateData.StopDef = input.StopDef;
                    await _fsql.Update<HisDataState>()
                        .Set(s => s.StopDef, input.StopDef)
                        .Set(s => s.StopId, (int?)null)
                        .Where(s => s.Id == stateData.Id)
                        .ExecuteAffrowsAsync();
                }

                return Ok(new ApiResponse<object>(200, null, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入设备 {DeviceId} 数据失败", input.Id);
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误"));
            }
        }
        [HttpGet("stop-reason", Name = "获取停机原因状态列表")]
        public async Task<IActionResult> GetStopReasonList()
        {
            try
            {
                _logger.LogInformation("开始获取停机原因列表");
                // 1. 查询所有停机原因
                var stopReasons = await _fsql.Select<StopState>()
                    .ToListAsync();
                return Ok(new ApiResponse<IEnumerable<StopState>>(200, stopReasons, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取停机原因列表失败");
                return StatusCode(500, new ApiResponse<object>(500, null, "服务器内部错误"));
            }
        }
    }
}
