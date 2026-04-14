using CpuSchedulingBackend.DTOs;
using CpuSchedulingBackend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CpuSchedulingBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulerController : ControllerBase
    {
        private readonly ISchedulerService _schedulerService;
        public SchedulerController(ISchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        [HttpPost("schedule")]
        public ActionResult<ScheduleResultDto> Schedule([FromBody] ScheduleRequestDto request)
        {
            if (request.Processes == null || request.Processes.Count == 0)
                return BadRequest("No processes provided.");

            var result = _schedulerService.Calculate(request);
                return Ok(result);
        }
    }
}
