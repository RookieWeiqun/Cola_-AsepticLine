using AutoMapper;
using Cola.DTO;
using Cola.Extensions;
using Cola.Model;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet(Name = "GetCheck")]
        public async Task<IActionResult> Get()
        {
            try
            {
                _logger.LogInformation("��ʼ��ȡ����������");

                var data = await _fsql.Select<HisDataCheck>().ToListAsync();

                _logger.LogInformation("�ɹ���ȡ���������ݣ�������{Count}", data.Count);

                //var dataDTO = _mapper.Map<IEnumerable<CheckPara>>(data);
                //ExcelHelper.ExportToExcel(data, Directory.GetCurrentDirectory());
                // ���ر�׼��Ӧ��ʽ
                return Ok(data);
                //return Ok(new ApiResponse<IEnumerable<HisDataCheck>>(200, data, "�ɹ�"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ����������ʱ��������");

                // ���ش�����Ӧ��ʽ
                return StatusCode(500, new ApiResponse<object>(500, null, "�������ڲ�����"));
            }
        }
    }
}
