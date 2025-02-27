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

        [HttpGet("currunt", Name = "��ȡ��ǰʱ��������")]
        public async Task<IActionResult> GetCurrentTimeCheckData([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("��ʼ��ȡ�豸 {DeviceId} �� {InputTime} �ļ������", deviceId, inputTime);

                // ================== ��һ���֣���ȡ��ǰʱ��ļ�¼ ==================
                var closestRecord = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime <= inputTime)  
                    .Include(c => c.DeviceInfo) // �����豸��Ϣ
                    .OrderByDescending(c => c.RecordTime)
                    .FirstAsync();
                if (closestRecord == null)
                {
                    return NotFound(new ApiResponse<object>(200, null, "δ�ҵ�����"));
                }

                // ================== �ڶ����֣���������ת�� ==================
                // 1. �ռ�����CheckPara��ID
                var allCheckParaIds = ((JObject)closestRecord.Data)
                    .Properties()
                    .Select(p => int.Parse(p.Name))
                    .Distinct()
                    .ToList();
                // 2. ������ѯCheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 3. �������
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

                return Ok(new ApiResponse<CheckDataResult>(200, resultItem, "�ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�豸 {DeviceId} ����ʧ�� | ����ʱ�䣺{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"�������ڲ�����{ex.Message}"));
            }
        }
        [HttpGet("sharp", Name = "��ȡ����ʱ��������")]
        public async Task<IActionResult> GetSharpTimeCheckData([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                // ================== ��һ���֣���ȡ������������ ==================
                // 1. ���ɵ�����������ʱ�䣨00:00, 01:00,...,23:00��
                var dayStart = inputTime.Date;
                var hourlyPoints = Enumerable.Range(0, 24)
                    .Select(h => dayStart.AddHours(h))
                    .ToList();

                // 2. ��ѯ������������
                var dayEnd = dayStart.AddDays(1);
                var allRecords = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime >= dayStart &&
                        c.RecordTime < dayEnd)
                    .ToListAsync();

                // 3. �ڱ��ش������ݣ��ҵ�ÿ�����㸽�������ݣ�ʱ�䴰�� ��1 ���ӣ�
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
                // ================== �ڶ����֣���������ת�� ==================
                // 2. �ռ�����CheckPara��ID
                var allCheckParaIds = hourlyDatas
                    .Where(r => r.Data != null)
                    .SelectMany(r => ((JObject)r.Data).Properties().Select(p => int.Parse(p.Name)))
                    .Distinct()
                    .ToList();
                // 3. ������ѯCheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 4. �������
                var results = new List<CheckDataResult>();

                foreach (var hourlyData in hourlyDatas)
                {
                    var resultItem = new CheckDataResult
                    {
                        Id = hourlyData.Id,
                        DeviceId = hourlyData.DeviceId,
                        LineId = hourlyData.LineId,
                        RecipeId = hourlyData.RecipeId,
                        //����ֻȡRecordTime��ʱ��
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
                return Ok(new ApiResponse<IEnumerable<CheckDataResult>>(200, results, "�ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�豸 {DeviceId} ����ʧ�� | ����ʱ�䣺{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"�������ڲ�����{ex.Message}"));

            }
        }

        [HttpGet("currunt/test", Name = "��ȡ��ǰʱ��������/���Խӿ�")]
        public async Task<IActionResult> GetCurrentTimeCheckData2([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                _logger.LogInformation("��ʼ��ȡ�豸 {DeviceId} �� {InputTime} �ļ������", deviceId, inputTime);

                // ================== ��һ���֣���ȡ��ǰʱ��ļ�¼ ==================
                var closestRecord = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime <= inputTime)
                    .Include(c => c.DeviceInfo) // �����豸��Ϣ
                    .OrderByDescending(c => c.RecordTime)
                    .FirstAsync();
                if (closestRecord == null)
                {
                    return NotFound(new ApiResponse<object>(200, null, "δ�ҵ�����"));
                }

                // ================== �ڶ����֣���������ת�� ==================
                // 1. �ռ�����CheckPara��ID
                var allCheckParaIds = ((JObject)closestRecord.Data)
                    .Properties()
                    .Select(p => int.Parse(p.Name))
                    .Distinct()
                    .ToList();
                // 2. ������ѯCheckPara
                var checkParas = await _fsql.Select<CheckPara>()
                    .Where(c => allCheckParaIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);
                // 3. ��ȡȥ�غ��keynames
                var keynames = await _fsql.Select<CheckPara>()
                    .Where(c => c.Checked == 1)
                    .Distinct()
                    .ToListAsync(c=>c.KeyName);
                // 4. �������
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
        [HttpGet("CheckParas", Name = "ͨ���豸Id��ȡ����б�")]
        public async Task<IActionResult> GetCheckParasByDeviceId([FromQuery] int deviceId)
        {
            var keynames = await _fsql.Select<CheckPara>()
                .Where(c => c.DeviceId == deviceId)
                .ToListAsync(c => new { c.AliasName, c.KeyName });

            return Ok(new ApiResponse<IEnumerable<object>>(200, keynames, "�ɹ�"));
        }
        [HttpGet("exportToExcel", Name = "������챨��")]
        public async Task<IActionResult> ExportToExcel([FromQuery] int deviceId, [FromQuery] DateTime inputTime)
        {
            try
            {
                // ��ȡ����ĵ������
                var dayStart = inputTime.Date;
                var dayEnd = dayStart.AddDays(1);
                var checkRecords = await _fsql.Select<HisDataCheck>()
                    .Where(c =>
                        c.DeviceId == deviceId &&
                        c.RecordTime >= dayStart &&
                        c.RecordTime < dayEnd)
                    .ToListAsync();

                // ת�����ݣ����������е�����ת���߼���
                var results = new List<CheckDataResult>();
                // ... ����ʹ�������е�����ת���߼� ...

                // ����Excel
                var excelBytes = ExcelHelper.ExportCheckDataToExcel(results, inputTime);

                // �����ļ�
                string fileName = $"����¼��_{inputTime:yyyyMMdd}.xlsx";
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����Excelʧ�� | �豸ID��{DeviceId}, ���ڣ�{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"�������ڲ�����{ex.Message}"));
            }
        }
        [HttpGet("template", Name = "获取点检表模板")]
        public IActionResult GetTemplate()
        {
            try
            {
                var excelBytes = ExcelHelper.CreateCheckDataTemplate();
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
