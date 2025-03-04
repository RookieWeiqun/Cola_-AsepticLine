using AutoMapper;
using Cola.DTO;
using Cola.Extensions;
using Cola.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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

        [HttpGet("currunt/previous", Name = "获取当前时间点检数据")]
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
        [HttpGet("sharp/previous", Name = "获取整点时间点检数据")]
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

        [HttpGet("currunt", Name = "获取当前时间点检数据/测试接口")]
        public async Task<IActionResult> GetCurrentTimeCheckData2([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 在 {InputTime} 的检查数据", deviceId, inputTime);
                // 获取设备列表中的第一个设备当作deviceId，后续看客户需求
                var deviceIds = await _fsql.Select<DeviceType>()
                    .Where(n => n.Id == deviceId)
                    .FirstAsync(n=>n.DeviceList);
                List<int> deviceIdList = deviceIds.Split(',')
                                  .Select(int.Parse)
                                  .ToList();
                deviceId = deviceIdList[0];

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
        [HttpGet("sharp", Name = "获取整点时间点检数据test")]
        public async Task<IActionResult> GetSharpTimeCheckData2([FromQuery] int deviceId, [FromQuery] DateTime inputTime, [FromQuery] int shift)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceId} 在 {InputTime} 的整点检查数据", deviceId, inputTime);
                // 获取设备列表中的第一个设备当作deviceId，后续看客户需求
                var deviceIds = await _fsql.Select<DeviceType>()
                    .Where(n => n.Id == deviceId)
                    .FirstAsync(n => n.DeviceList);
                List<int> deviceIdList = deviceIds.Split(',')
                                  .Select(int.Parse)
                                  .ToList();
                deviceId = deviceIdList[0];
                // ================== 第一部分：获取当日整点数据 ==================
                // 1. 生成当日所有整点时间（00:00, 01:00,...,23:00）
                var dayStart = inputTime.Date;
       
                List<DateTime> hourlyPoints = new List<DateTime>();
                if (shift == 0)
                {
                    hourlyPoints = Enumerable.Range(7, 12)
                        .Select(h => dayStart.AddHours(h)) // 7:00 ~ 18:00
                        .ToList();
                }
                // Shift 1: 晚7点至次日早7点（共12小时）
                else if (shift == 1)
                {
                    hourlyPoints = Enumerable.Range(19, 5) // 当日19:00 ~ 23:00（5小时）
                        .Select(h => dayStart.AddHours(h))
                        .Concat(Enumerable.Range(0, 7) // 次日0:00 ~ 6:00（7小时）
                            .Select(h => dayStart.AddDays(1).AddHours(h)))
                        .ToList();
                }
                else 
                {
                    hourlyPoints = Enumerable.Range(0, 24)
                        .Select(h => dayStart.AddHours(h))
                        .ToList();
                }
                // 2. 查询当天7点到第二天7点的所有数据
                dayStart = inputTime.Date.AddHours(7);
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
        public async Task<IActionResult> GetCheckParasByDeviceId([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            // ================== 第一部分：获取表头数据 ==================
            var dayStart = inputTime.Date;
            var dayEnd = dayStart.AddDays(1);
            //获取设备7和9的点检项
            var recipeDetailList = await _fsql.Select<RecipeDetailInfo>()
                .ToListAsync();
            //获取当前时间的记录来获取recipe_id
            var allRecords = await _fsql.Select<HisDataCheck>()
             .Where(c =>
                 c.DeviceId == deviceId &&
                 c.RecordTime >= dayStart &&
                 c.RecordTime < dayEnd)
             .OrderBy(c => c.RecordTime)
             .ToListAsync();
            //取allRecords最后一个recordtime的recipe_id
            var recipeId = allRecords.LastOrDefault()?.RecipeId;
            var keynames = await _fsql.Select<CheckPara>()
                .Where(c => c.DeviceId == deviceId)
                .ToListAsync();
            List<CheckHeadItem> checkHeads  = new List<CheckHeadItem>();
            foreach (var name in keynames )
            {
               var item = new CheckHeadItem
                {
                    ProjectDescription = name.AliasName,
                    ReferenceValue = recipeDetailList.FirstOrDefault(r => r.RecipeId == recipeId && r.CheckParaId == name.Id)?.Lower ?? "null",
                    Unit = name.Unit,
                    ProjectName = name.Name,
                    Keyname = name.KeyName
                };
                   
                checkHeads.Add(item);
            }

            return Ok(new ApiResponse<IEnumerable<object>>(200, checkHeads, "成功"));
        }


        [HttpGet("deviceList", Name = "设备列表")]
        public async Task<IActionResult> GetDeviceList()
        {
            try
            {
                var devicelist = await _fsql.Select<DeviceType>()
                    .Where(c => c.Report == 1)
                    .ToListAsync(c=> new{ c.Id,c.Name});
                    return Ok(new ApiResponse<IEnumerable<object>>(200, devicelist, "成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设别列表失败");
                return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));
            }
        }
        //[HttpGet("Export/Juice", Name = "导出果汁excel数据")]
        //public async Task<IActionResult> ExportToExcel2( [FromQuery] DateTime inputTime, [FromQuery] string shift)
        //{
        //    try
        //    {
        //        // ================== 第一部分：获取表头数据 ==================
        //        var dayStart = inputTime.Date;
        //        var dayEnd = dayStart.AddDays(1);
        //        //获取设备7和9的点检项
        //        var keynames = await _fsql.Select<CheckPara>()
        //            .Where(c =>  c.DeviceId == 9)
        //            .OrderBy(o => o.DeviceId)
        //            .ToListAsync();
        //        var recipeDetailList = await _fsql.Select<RecipeDetailInfo>()
        //            .ToListAsync();
        //        //获取当前时间的记录
        //        var allRecords = await _fsql.Select<HisDataCheck>()
        //         .Where(c =>
        //             c.DeviceId == 9 &&
        //             c.RecordTime >= dayStart &&
        //             c.RecordTime < dayEnd)
        //         .OrderBy(c => c.RecordTime)
        //         .ToListAsync();
        //        //取allRecords最后一个recordtime的recipe_id
        //        var recipeId = allRecords.LastOrDefault()?.RecipeId;

        //        // ================== 第二部分：获取整点列数据 ==================
        //        var result = await GetSharpTimeForExcel(7, inputTime,0);
        //        var result2 = await GetSharpTimeForExcel(9, inputTime, 0);
        //        var deviceStepList = (await _fsql.Select<DeviceStep>()
        //            .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);
        //        if (result is not null)
        //        {
        //            // Prepare data for ExportCheckDataToExcel method
        //            var excelDataList = new List<ExcelData>();

        //            foreach (var item in result2)
        //            {
        //                // Convert CheckDataItem to JObject
        //                var dataJson = JsonConvert.SerializeObject(item.Data);
        //                var dataDict = JObject.Parse(dataJson).Properties()
        //                    .Where(p => p.Value.Type != JTokenType.Null && p.Value.ToObject<float>() != -1)
        //                    .ToDictionary(p => p.Name, p => p.Value);

        //                foreach (var prop in dataDict)
        //                {
        //                    var checkPara = keynames.FirstOrDefault(k => k.KeyName == prop.Key);
        //                    if (checkPara != null)
        //                    {
        //                        var existingExcelData = excelDataList.FirstOrDefault(e =>
        //                            e.DeviceName == checkPara.DeviceId.ToString() &&
        //                            e.ProjectDescription == checkPara.AliasName &&
        //                            e.ReferenceValue == (recipeDetailList.FirstOrDefault(r => r.RecipeId == recipeId && r.CheckParaId == checkPara.Id)?.Lower ?? "null") &&
        //                            e.Unit == checkPara.Unit &&
        //                            e.ProjectName == checkPara.Name);

        //                        if (existingExcelData == null)
        //                        {
        //                            var excelData = new ExcelData
        //                            {
        //                                DeviceName = checkPara.DeviceId.ToString(),
        //                                ProjectDescription = checkPara.AliasName,
        //                                ReferenceValue = recipeDetailList.FirstOrDefault(r => r.RecipeId == recipeId && r.CheckParaId == checkPara.Id)?.Lower ?? "null",
        //                                Unit = checkPara.Unit,
        //                                ProjectName = checkPara.Name
        //                            };
        //                            // Add time-based values to the dictionary
        //                            if (prop.Key == "MixerStep")
        //                            {
        //                                var mixerStepId = prop.Value.ToObject<int>();
        //                                if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
        //                                {
        //                                    excelData.TimeValues[item.RecordTime] = mixerStepName;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                excelData.TimeValues[item.RecordTime] = prop.Value.ToObject<float>().ToString();
        //                            }
        //                            excelDataList.Add(excelData);
        //                        }
        //                        else
        //                        {
        //                            if (prop.Key == "MixerStep")
        //                            {
        //                                var mixerStepId = prop.Value.ToObject<int>();
        //                                if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
        //                                {
        //                                    existingExcelData.TimeValues[item.RecordTime] = mixerStepName;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                existingExcelData.TimeValues[item.RecordTime] = prop.Value.ToObject<float>().ToString();
        //                            }
        //                            // Add time-based values to the existing dictionary
        //                        }
        //                    }
        //                }
        //            }

        //            // Call the ExportCheckDataToExcel method
        //            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "果肉杀菌在线混合记录表模板.xlsx");
        //            var excelBytes = ExcelHelper.ExportCheckDataToExcel(templatePath, excelDataList,inputTime,shift);
        //            string fileName = $"果肉杀菌在线混合记录表模板.xlsx";
        //            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        //        }
        //        else
        //        {
        //            return NotFound(new ApiResponse<object>(200, null, "未找到数据"));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "导出模板失败");
        //        return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));
        //    }
        //}





        //private async Task<List<CheckDataResult>> GetSharpTimeForExcel(int deviceId, DateTime inputTime, int shift)
        //{
        //    try
        //    {
        //        _logger.LogInformation("开始获取设备 {deviceId} 在 {InputTime} 的整点检查数据",  deviceId, inputTime);

        //        // ================== 第一部分：获取当日整点数据 ==================
        //        // 1. 生成当日所有整点时间（00:00, 01:00,...,23:00）
        //        var dayStart = inputTime.Date;
        //        List<DateTime> hourlyPoints = new List<DateTime>();
        //        if (shift == 0)
        //        {
        //            hourlyPoints = Enumerable.Range(7, 12)
        //               .Select(h => dayStart.AddHours(h))
        //               .ToList();
        //        }
        //        else if (shift == 1)
        //        {
        //            hourlyPoints = Enumerable.Range(19, 6)
        //                .Select(h => dayStart.AddHours(h))
        //                .Concat(Enumerable.Range(0, 7).Select(h => dayStart.AddDays(1).AddHours(h)))
        //                .ToList();
        //        }
        //        // 2. 查询当天所有数据
        //        var dayEnd = dayStart.AddDays(1);
        //        var allRecords = await _fsql.Select<HisDataCheck>()
        //            .Where(c =>
        //                c.DeviceId==deviceId &&
        //                c.RecordTime >= dayStart &&
        //                c.RecordTime < dayEnd)
        //            .ToListAsync();

        //        // 3. 在本地处理数据，找到每个整点附近的数据（时间窗口 ±1 分钟）
        //        var hourlyDatas = new List<HisDataCheck>();
        //        foreach (var hour in hourlyPoints)
        //        {
        //            var start = hour.AddSeconds(-60);
        //            var end = hour.AddSeconds(60);

        //            var recordsInWindow = allRecords
        //                .Where(c => c.RecordTime >= start && c.RecordTime <= end)
        //                .OrderBy(c => Math.Abs((c.RecordTime - hour).Value.Ticks))
        //                .FirstOrDefault();

        //            if (recordsInWindow != null)
        //            {
        //                hourlyDatas.Add(recordsInWindow);
        //            }
        //        }

        //        // ================== 第二部分：处理数据转换 ==================
        //        // 1. 收集所有CheckPara的ID
        //        var allCheckParaIds = hourlyDatas
        //            .Where(r => r.Data != null)
        //            .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
        //            .Distinct()
        //            .ToList();
        //        // 2. 批量查询CheckPara
        //        var checkParas = await _fsql.Select<CheckPara>()
        //            .Where(c => allCheckParaIds.Contains(c.Id))
        //            .ToDictionaryAsync(c => c.Id);
        //        // 3. 获取去重后的keynames
        //        var keynames = await _fsql.Select<CheckPara>()
        //            .Where(c => c.Checked == 1)
        //            .Distinct()
        //            .ToListAsync(c => c.KeyName);

        //        // 4. 构建结果
        //        var results = new List<CheckDataResult>();
        //        var checkDataItemType = typeof(CheckDataItem);

        //        foreach (var hourlyData in hourlyDatas)
        //        {
        //            var resultItem = new CheckDataResult
        //            {
        //                Id = hourlyData.Id,
        //                DeviceId = hourlyData.DeviceId,
        //                LineId = hourlyData.LineId,
        //                RecipeId = hourlyData.RecipeId,
        //                // 只取RecordTime的时分
        //                RecordTime = hourlyData.RecordTime?.ToString("HH:mm"),
        //            };

        //            if (hourlyData.Data != null)
        //            {
        //                var dataDict = (JObject)hourlyData.Data;
        //                var checkDataItem = new CheckDataItem();
        //                foreach (var prop in dataDict.Properties())
        //                {
        //                    var checkParaId = int.Parse(prop.Name);
        //                    if (checkParas.TryGetValue(checkParaId, out var checkPara))
        //                    {
        //                        if (keynames.Contains(checkPara.KeyName))
        //                        {
        //                            var property = checkDataItemType.GetProperty(checkPara.KeyName);
        //                            if (property != null && property.PropertyType == typeof(float))
        //                            {
        //                                property.SetValue(checkDataItem, prop.Value.ToObject<float>());
        //                            }
        //                            else if (property != null && property.PropertyType == typeof(int))
        //                            {
        //                                property.SetValue(checkDataItem, prop.Value.ToObject<int>());
        //                            }
        //                        }
        //                    }
        //                }
        //                resultItem.Data = checkDataItem;
        //            }

        //            results.Add(resultItem);
        //        }
        //        return results;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "获取设备 {deviceId} 数据失败 | 输入时间：{InputTime}",  deviceId, inputTime);
        //        return null;
        //    }
        //}

        [HttpGet("Export/Juice", Name = "导出果汁excel数据")]
        public async Task<IActionResult> ExportToExcel2([FromQuery] DateTime inputTime, [FromQuery] int shift)
        {
            try
            {
                var deviceIds = new List<int> { 7, 9 };
                var excelDataList = await GetSharpTimeForExcel(deviceIds, inputTime, shift);

                if (excelDataList != null)
                {
                    string templatePath = "";
                    if (shift == 0) 
                    {
                        templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "果肉杀菌在线混合记录表模板_白班.xlsx");
                    }
                    else if (shift == 1)
                    {
                        templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Template", "果肉杀菌在线混合记录表模板_夜班.xlsx");
                    }
                    var excelBytes = ExcelHelper.ExportCheckDataToExcel(templatePath, excelDataList, inputTime);
                    string fileName = $"果肉杀菌在线混合记录表.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    return NotFound(new ApiResponse<object>(200, null, "未找到数据"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出模板失败");
                return StatusCode(500, new ApiResponse<object>(500, null, $"服务器内部错误：{ex.Message}"));
            }
        }
        private async Task<List<HisDataCheck>> GetHourlyData(List<int> deviceIds, DateTime inputTime, int shift)
        {
            var dayStart = inputTime.Date;
            List<DateTime> hourlyPoints = new List<DateTime>();
            if (shift == 0)
            {
                hourlyPoints = Enumerable.Range(7, 12)
                    .Select(h => dayStart.AddHours(h)) // 7:00 ~ 18:00
                    .ToList();
            }
            // Shift 1: 晚7点至次日早7点（共12小时）
            else if (shift == 1)
            {
                hourlyPoints = Enumerable.Range(19, 5) // 当日19:00 ~ 23:00（5小时）
                    .Select(h => dayStart.AddHours(h))
                    .Concat(Enumerable.Range(0, 7) // 次日0:00 ~ 6:00（7小时）
                        .Select(h => dayStart.AddDays(1).AddHours(h)))
                    .ToList();
            }
            dayStart = dayStart.AddHours(7);
            var dayEnd = dayStart.AddDays(1);
            var allRecords = await _fsql.Select<HisDataCheck>()
                .Where(c =>
                    deviceIds.Contains(c.DeviceId.Value) &&
                    c.RecordTime >= dayStart &&
                    c.RecordTime < dayEnd)
                .ToListAsync();
            // 按DeviceId分组记录
            var recordsByDevice = allRecords
                .GroupBy(c => c.DeviceId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            var hourlyDatas = new List<HisDataCheck>();
            foreach (var hour in hourlyPoints)
            {
                var start = hour.AddSeconds(-60);
                var end = hour.AddSeconds(60);

                foreach (var deviceId in deviceIds)
                {
                    // 获取当前设备的记录
                    if (recordsByDevice.TryGetValue(deviceId, out var deviceRecords))
                    {
                        // 在当前设备记录中查找时间窗口内的数据
                        var recordsInWindow = deviceRecords
                            .Where(c => c.RecordTime >= start && c.RecordTime <= end)
                            .OrderBy(c => Math.Abs((c.RecordTime - hour).Value.Ticks))
                            .FirstOrDefault();

                        if (recordsInWindow != null)
                        {
                            hourlyDatas.Add(recordsInWindow);
                        }
                    }
                }
                
            }

            return hourlyDatas;
        }
        private async Task<List<ExcelData>> GetSharpTimeForExcel(List<int> deviceIds, DateTime inputTime, int shift)
        {
            try
            {
                _logger.LogInformation("开始获取设备 {DeviceIds} 在 {InputTime} 的整点检查数据", string.Join(",", deviceIds), inputTime);

                var hourlyDatas = await GetHourlyData(deviceIds, inputTime, shift);

                var allCheckParaIds = hourlyDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();

                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToListAsync();

                var recipeDetailList = await _fsql.Select<RecipeDetailInfo>()
                    .ToListAsync();

                var deviceStepList = (await _fsql.Select<DeviceStep>()
                    .ToListAsync()).ToDictionary(dt => dt.Id, dt => dt.Name);

                var deviceList = await _fsql.Select<DeviceInfo>()
                    .ToListAsync();

                var excelDataList = new List<ExcelData>();
                var res = hourlyDatas.Where(n => n.DeviceId == 9).ToList();
                foreach (var hourlyData in hourlyDatas)
                {
                    var dataJson = JsonConvert.SerializeObject(hourlyData.Data);
                    var dataDict = JObject.Parse(dataJson).Properties()
                        //.Where(p => p.Value.Type != JTokenType.Null && p.Value.ToObject<float>() != 0)
                        .ToDictionary(p => p.Name, p => p.Value);

                    foreach (var prop in dataDict)
                    {
                        var checkPara = checkParas.FirstOrDefault(k => k.Id.ToString() == prop.Key);
                        if (checkPara != null)
                        {
                            var existingExcelData = excelDataList.FirstOrDefault(e =>
                                e.DeviceName == deviceList.Where(n => n.Id == checkPara.DeviceId).FirstOrDefault().Name &&
                                e.ProjectDescription == checkPara.AliasName &&
                                e.ReferenceValue == (recipeDetailList.FirstOrDefault(r => r.RecipeId == hourlyData.RecipeId && r.CheckParaId == checkPara.Id)?.Lower ?? "null") &&
                                e.Unit == checkPara.Unit &&
                                e.ProjectName == checkPara.Name);

                            if (existingExcelData == null)
                            {
                                var excelData = new ExcelData
                                {
                                    DeviceName = deviceList.Where(n=>n.Id== checkPara.DeviceId).FirstOrDefault().Name,
                                    ProjectDescription = checkPara.AliasName,
                                    ReferenceValue = recipeDetailList.FirstOrDefault(r => r.RecipeId == hourlyData.RecipeId && r.CheckParaId == checkPara.Id)?.Lower ?? "null",
                                    Unit = checkPara.Unit,
                                    ProjectName = checkPara.Name
                                };
                                if (prop.Key == "MixerStep")
                                {
                                    var mixerStepId = prop.Value.ToObject<int>();
                                    if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                    {
                                        excelData.TimeValues[hourlyData.RecordTime?.ToString("HH:mm")] = mixerStepName;
                                    }
                                }
                                else
                                {
                                    excelData.TimeValues[hourlyData.RecordTime?.ToString("HH:mm")] = prop.Value.ToObject<float>().ToString();
                                }
                                excelDataList.Add(excelData);
                            }
                            else
                            {
                                if (prop.Key == "MixerStep")
                                {
                                    var mixerStepId = prop.Value.ToObject<int>();
                                    if (deviceStepList.TryGetValue(mixerStepId, out var mixerStepName))
                                    {
                                        existingExcelData.TimeValues[hourlyData.RecordTime?.ToString("HH:mm")] = mixerStepName;
                                    }
                                }
                                else
                                {
                                    existingExcelData.TimeValues[hourlyData.RecordTime?.ToString("HH:mm")] = prop.Value.ToObject<float>().ToString();
                                }
                            }
                        }
                    }
                }

                return excelDataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备 {DeviceIds} 数据失败 | 输入时间：{InputTime}", string.Join(",", deviceIds), inputTime);
                return null;
            }
        }

    }
}
