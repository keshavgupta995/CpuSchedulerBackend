using CpuSchedulingBackend.DTOs;

namespace CpuSchedulingBackend.Interfaces
{
    public interface ISchedulerService
    {
        ScheduleResultDto Calculate(ScheduleRequestDto request);
    }
}
