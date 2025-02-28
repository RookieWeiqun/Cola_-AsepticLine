using AutoMapper;
using Cola.DTO;
using Cola.Extensions;
using Cola.Model;
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
                    .Include(c => c.DeviceInfo) // �����豸��Ϣ
                    .OrderByDescending(c => c.RecordTime)
                    .FirstAsync();
                if (closestRecord == null)
                {
                    return NotFound(new ApiResponse<object>(200, null, "未找到数据"));
                }

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
                    RecordTime = closestRecord.RecordTime?.ToString(),
                };
                if (closestRecord.Data != null)
                {
                    var dataDict = (JObject)closestRecord.Data;
                    var checkDataItem = new CheckDataItem();
                    foreach (var prop in dataDict.Properties())
                    {
                        var checkParaId = int.Parse(prop.Name);
                        if (checkParas.TryGetValue(checkParaId, out var checkPara))
                        {
                            // Assign values to CheckDataItem based on KeyName
                            switch (checkPara.KeyName)
                            {
                                case CheckPara_KeyName.AsepticTankTopPressure:
                                    checkDataItem.AsepticTankTopPressure = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.CoolingPressureDifference:
                                    checkDataItem.CoolingPressureDifference = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.CoolingSectionPressure01:
                                    checkDataItem.CoolingSectionPressure01 = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.CoolingSectionPressure02:
                                    checkDataItem.CoolingSectionPressure02 = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.CoolingTemperature:
                                    checkDataItem.CoolingTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.CrossTemperature:
                                    checkDataItem.CrossTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.DegassingTankLiquidLevel:
                                    checkDataItem.DegassingTankLiquidLevel = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.DegassingTankPressure:
                                    checkDataItem.DegassingTankPressure = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.DegassingTankTemperature:
                                    checkDataItem.DegassingTankTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.EndTemperature:
                                    checkDataItem.EndTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.HoldingTemperature:
                                    checkDataItem.HoldingTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.IceWaterPressureDifference:
                                    checkDataItem.IceWaterPressureDifference = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.LiquidLevel:
                                    checkDataItem.LiquidLevel = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.MixerBottomPressure:
                                    checkDataItem.MixerBottomPressure = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.MixerStep:
                                    checkDataItem.MixerStep = prop.Value.ToObject<int>();
                                    break;
                                case CheckPara_KeyName.MixerTopPressure:
                                    checkDataItem.MixerTopPressure = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.ProductFlowRate:
                                    checkDataItem.ProductFlowRate = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.ProductPressure:
                                    checkDataItem.ProductPressure = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.ProductTemperature:
                                    checkDataItem.ProductTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.RoomTemperature:
                                    checkDataItem.RoomTemperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.SterilizationTime:
                                    checkDataItem.SterilizationTime = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.Temperature:
                                    checkDataItem.Temperature = prop.Value.ToObject<float>();
                                    break;
                                case CheckPara_KeyName.TowerWaterPressureDifference:
                                    checkDataItem.TowerWaterPressureDifference = prop.Value.ToObject<float>();
                                    break;
                            }
                        }
                    }
                    resultItem.Data = checkDataItem;
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
                        //这里只取RecordTime的时分
                        RecordTime = hourlyData.RecordTime?.ToString("HH:mm"),

                    };

                    if (hourlyData.Data != null)
                    {
                        var dataDict = (JObject)hourlyData.Data;
                        var checkDataItem = new CheckDataItem();
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                // Assign values to CheckDataItem based on KeyName
                                switch (checkPara.KeyName)
                                {
                                    case CheckPara_KeyName.AsepticTankTopPressure:
                                        checkDataItem.AsepticTankTopPressure = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CoolingPressureDifference:
                                        checkDataItem.CoolingPressureDifference = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CoolingSectionPressure01:
                                        checkDataItem.CoolingSectionPressure01 = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CoolingSectionPressure02:
                                        checkDataItem.CoolingSectionPressure02 = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CoolingTemperature:
                                        checkDataItem.CoolingTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.CrossTemperature:
                                        checkDataItem.CrossTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.DegassingTankLiquidLevel:
                                        checkDataItem.DegassingTankLiquidLevel = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.DegassingTankPressure:
                                        checkDataItem.DegassingTankPressure = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.DegassingTankTemperature:
                                        checkDataItem.DegassingTankTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.EndTemperature:
                                        checkDataItem.EndTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.HoldingTemperature:
                                        checkDataItem.HoldingTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.IceWaterPressureDifference:
                                        checkDataItem.IceWaterPressureDifference = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.LiquidLevel:
                                        checkDataItem.LiquidLevel = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.MixerBottomPressure:
                                        checkDataItem.MixerBottomPressure = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.MixerStep:
                                        checkDataItem.MixerStep = prop.Value.ToObject<int>();
                                        break;
                                    case CheckPara_KeyName.MixerTopPressure:
                                        checkDataItem.MixerTopPressure = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.ProductFlowRate:
                                        checkDataItem.ProductFlowRate = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.ProductPressure:
                                        checkDataItem.ProductPressure = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.ProductTemperature:
                                        checkDataItem.ProductTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.RoomTemperature:
                                        checkDataItem.RoomTemperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.SterilizationTime:
                                        checkDataItem.SterilizationTime = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.Temperature:
                                        checkDataItem.Temperature = prop.Value.ToObject<float>();
                                        break;
                                    case CheckPara_KeyName.TowerWaterPressureDifference:
                                        checkDataItem.TowerWaterPressureDifference = prop.Value.ToObject<float>();
                                        break;
                                }
                            }
                        }
                        resultItem.Data = checkDataItem;
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

        [HttpGet("currunt/test", Name = "获取当前时间点检数据/测试接口")]
        public async Task<IActionResult> GetCurrentTimeCheckData2([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 在 {InputTime} 的检查数据", deviceId, inputTime);

                // ================== 第一部分：获取当前时间的记录 ==================
                var closestRecord = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime <= inputTime)
                    .Include(c => c.DeviceInfo) 
                    .OrderByDescending(c => c.RecordTime)
                    .FirstAsync();
                if (closestRecord == null)
                {
                    return NotFound(new ApiResponse<object>(200, null, "未找到数据"));
                }

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
                // 3. 获取去重后的keynames
                var keynames = await _fsql.Select<CheckPara>()
                    .Where(c => c.Checked == 1)
                    .Distinct()
                    .ToListAsync(c=>c.KeyName);
                // 4. 构建结果
                var resultItem = new CheckDataResult
                {
                    Id = closestRecord.Id,
                    DeviceId = closestRecord.DeviceId,
                    LineId = closestRecord.LineId,
                    RecipeId = closestRecord.RecipeId,
                    RecordTime = closestRecord.RecordTime?.ToString(),
                };
                if (closestRecord.Data != null)
                {
                    var dataDict = (JObject)closestRecord.Data;
                    var checkDataItem = new CheckDataItem();
                    var checkDataItemType = typeof(CheckDataItem);
                    foreach (var prop in dataDict.Properties())
                    {
                        var checkParaId = int.Parse(prop.Name);
                        if (checkParas.TryGetValue(checkParaId, out var checkPara))
                        {
                            if (keynames.Contains(checkPara.KeyName))
                            {
                                var property = checkDataItemType.GetProperty(checkPara.KeyName);
                                if (property != null && property.PropertyType == typeof(float))
                                {
                                    property.SetValue(checkDataItem, prop.Value.ToObject<float>());
                                }
                                else if (property != null && property.PropertyType == typeof(int))
                                {
                                    property.SetValue(checkDataItem, prop.Value.ToObject<int>());
                                }
                            }
                        }
                    }
                    resultItem.Data = checkDataItem;
                }

                return Ok(new ApiResponse<CheckDataResult>(200, resultItem, "�ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�豸 {DeviceId} ����ʧ�� | ����ʱ�䣺{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"�������ڲ�����{ex.Message}"));
            }
        }
        [HttpGet("sharp/test", Name = "获取整点时间点检数据test")]
        public async Task<IActionResult> GetSharpTimeCheckData2([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 在 {InputTime} 的整点检查数据", deviceId, inputTime);

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
                // 1. 收集所有CheckPara的ID
                var allCheckParaIds = hourlyDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 2. 批量查询CheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 3. 获取去重后的keynames
                var keynames = await _fsql.Select<CheckPara>()
                    .Where(c => c.Checked == 1)
                    .Distinct()
                    .ToListAsync(c => c.KeyName);

                // 4. 构建结果
                var results = new List<CheckDataResult>();
                var checkDataItemType = typeof(CheckDataItem);

                foreach (var hourlyData in hourlyDatas)
                {
                    var resultItem = new CheckDataResult
                    {
                        Id = hourlyData.Id,
                        DeviceId = hourlyData.DeviceId,
                        LineId = hourlyData.LineId,
                        RecipeId = hourlyData.RecipeId,
                        // 只取RecordTime的时分
                        RecordTime = hourlyData.RecordTime?.ToString("HH:mm"),
                    };

                    if (hourlyData.Data != null)
                    {
                        var dataDict = (JObject)hourlyData.Data;
                        var checkDataItem = new CheckDataItem();
                        foreach (var prop in dataDict.Properties())
                        {
                            var checkParaId = int.Parse(prop.Name);
                            if (checkParas.TryGetValue(checkParaId, out var checkPara))
                            {
                                if (keynames.Contains(checkPara.KeyName))
                                {
                                    var property = checkDataItemType.GetProperty(checkPara.KeyName);
                                    if (property != null && property.PropertyType == typeof(float))
                                    {
                                        property.SetValue(checkDataItem, prop.Value.ToObject<float>());
                                    }
                                    else if (property != null && property.PropertyType == typeof(int))
                                    {
                                        property.SetValue(checkDataItem, prop.Value.ToObject<int>());
                                    }
                                }
                            }
                        }
                        resultItem.Data = checkDataItem;
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

        [HttpGet("CheckParas", Name = "通过设备Id获取点检列表")]
        public async Task<IActionResult> GetCheckParasByDeviceId([FromQuery] int deviceId)
        {
            var keynames = await _fsql.Select<CheckPara>()
                .Where(c => c.DeviceId == deviceId)
                .ToListAsync(c => new { c.AliasName, c.KeyName });

            return Ok(new ApiResponse<IEnumerable<object>>(200, keynames, "成功"));
        }

       
        [HttpGet("Export", Name = "导出excel数据")]
        public async Task<IActionResult> ExportToExcel2( [FromQuery] DateTime inputTime)
        {
            try
            {

                var dayStart = inputTime.Date;
                var hourlyPoints = Enumerable.Range(0, 24)
                   .Select(h => dayStart.AddHours(h))
                   .ToList();

                var dayEnd = dayStart.AddDays(1);
                var keynames = await _fsql.Select<CheckPara>()
                    .Where(c => c.DeviceId == 7||c.DeviceId==9)
                    .OrderBy(o=>o.DeviceId)
                    .ToListAsync();
                var recipeDetailList = await _fsql.Select<RecipeDetailInfo>()
                    .ToListAsync();
                //获取当前时间的记录
                var allRecords = await _fsql.Select<HisDataCheck>()
                 .Where(c =>
                     c.DeviceId == 7 &&
                     c.RecordTime >= dayStart &&
                     c.RecordTime < dayEnd)
                 .OrderBy(c => c.RecordTime)
                 .ToListAsync();
                //取allRecords最后一个recordtime的recipe_id
                var recipeId = allRecords.LastOrDefault()?.RecipeId;
                // 3. 在本地处理数据，找到每个整点附近的数据（时间窗口 ±1 分钟）
                var hourlyDatas = new List<HisDataCheck>();
                var ExcelDatas = new List<ExcelData>();
                foreach (var key in keynames) 
                {
                    var excelData = new ExcelData
                    {
                        DeviceName = key.DeviceId.ToString(),
                        ProjectDescription = key.AliasName,
                        ReferenceValue = recipeDetailList.Where(n => n.RecipeId == recipeId && n.CheckParaId == key.Id).FirstOrDefault() != null ? recipeDetailList.Where(n => n.RecipeId == recipeId && n.CheckParaId == key.Id).FirstOrDefault().Lower : "null",
                        Unit = key.Unit,
                        ProjectName = key.Name
                    };
                   
                    ExcelDatas.Add(excelData);

                }
                //foreach (var hour in hourlyPoints)
                //{
                //    var start = hour.AddSeconds(-60);
                //    var end = hour.AddSeconds(60);

                //    var recordsInWindow = allRecords
                //        .Where(c => c.RecordTime >= start && c.RecordTime <= end)
                //        .OrderBy(c => Math.Abs((c.RecordTime - hour).Value.Ticks))
                //        .FirstOrDefault();

                //    if (recordsInWindow != null)
                //    {
                //        hourlyDatas.Add(recordsInWindow);
                //    }
                //}
                var results = new List<CheckDataResult>();
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Template", "果肉杀菌在线混合记录表模板.xlsx");
                var excelBytes = ExcelHelper.ExportCheckDataToExcel(path,ExcelDatas);
                string fileName = $"果肉杀菌在线混合记录表模板.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出模板失败");
                return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));
            }
        }
    }
}
