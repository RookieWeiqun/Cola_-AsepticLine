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
                _logger.LogInformation("开始获取检查参数数据");

                var data = await _fsql.Select<HisDataCheck>().ToListAsync();

                _logger.LogInformation("成功获取检查参数数据，数量：{Count}", data.Count);

                //var dataDTO = _mapper.Map<IEnumerable<CheckPara>>(data);
                //ExcelHelper.ExportToExcel(data, Directory.GetCurrentDirectory());
                // 返回标准响应格式
                return Ok(data);
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
