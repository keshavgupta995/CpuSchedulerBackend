namespace CpuSchedulingBackend.DTOs
{
    public class ScheduleRequestDto
    {
        public List<ProcessInputDto> Processes { get; set; } = new();
        public string Algorithm { get; set; } = string.Empty;
        public int? Quantum { get; set; }
    }
}
