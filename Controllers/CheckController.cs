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
                return Ok(new ApiResponse<IEnumerable<CheckDataResult>>(200, results, "�ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ�豸 {DeviceId} ����ʧ�� | ����ʱ�䣺{InputTime}", deviceId, inputTime);
                return StatusCode(500, new ApiResponse<object>(500, null, $"�������ڲ�����{ex.Message}"));

            }
        }
    }
}
