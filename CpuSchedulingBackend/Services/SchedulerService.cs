using CpuSchedulingBackend.DTOs;
using CpuSchedulingBackend.Interfaces;

namespace CpuSchedulingBackend.Services
{
    public class SchedulerService : ISchedulerService
    {
        public ScheduleResultDto Calculate(ScheduleRequestDto request)
        {
            return request.Algorithm.ToUpper() switch
            {
                "FCFS" => RunFCFS(request.Processes),
                "RR" => RunRoundRobin(request.Processes, request.Quantum ?? 2),
                "SJF" => RunSJF(request.Processes),
                "SRTF" => RunSRTF(request.Processes),
                "PRIORITY" => RunPriority(request.Processes),
                "PRIORITY_PREEMPTIVE" => RunPriorityPreemptive(request.Processes),
                _ => throw new ArgumentException($"Unknown algorithm: {request.Algorithm}")
            };
        }

        private ScheduleResultDto RunFCFS(List<ProcessInputDto> processes)
        {
            // Sort by arrival time
            var sorted = processes.OrderBy(p => p.ArrivalTime).ToList();

            var timeline = new List<TimeSlotDto>();
            int currentTime = 0;

            foreach (var process in sorted)
            {
                // If CPU is idle, jump to process arrival
                if (currentTime < process.ArrivalTime)
                    currentTime = process.ArrivalTime;

                timeline.Add(new TimeSlotDto
                {
                    ProcessId = process.ProcessId,
                    StartTime = currentTime,
                    EndTime = currentTime + process.BurstTime
                });

                currentTime += process.BurstTime;
            }

            return BuildResult(timeline, processes);
        }

        private ScheduleResultDto RunRoundRobin(List<ProcessInputDto> processes, int quantum)
        {
            var remainingBurst = processes.ToDictionary(
                p => p.ProcessId,
                p => p.BurstTime
            );

            var timeline = new List<TimeSlotDto>();
            var queue = new Queue<ProcessInputDto>();
            var arrived = new HashSet<string>();
            int currentTime = 0;

            var sortedByArrival = processes.OrderBy(p => p.ArrivalTime).ToList();

            void EnqueueNewArrivals(int upToTime)
            {
                foreach (var p in sortedByArrival
                    .Where(p => p.ArrivalTime <= upToTime && !arrived.Contains(p.ProcessId)))
                {
                    queue.Enqueue(p);
                    arrived.Add(p.ProcessId);
                }
            }

            // Enqueue all processes that have arrived at t=0
            EnqueueNewArrivals(currentTime);

            while (queue.Count > 0 || arrived.Count < processes.Count)
            {
                // If queue is empty but processes haven't arrived yet, jump to next arrival
                if (queue.Count == 0)
                {
                    currentTime = sortedByArrival
                        .First(p => !arrived.Contains(p.ProcessId)).ArrivalTime;
                    EnqueueNewArrivals(currentTime);
                    continue;
                }

                var process = queue.Dequeue();
                int burst = remainingBurst[process.ProcessId];
                int runTime = Math.Min(burst, quantum);

                int startTime = currentTime;
                currentTime += runTime;
                remainingBurst[process.ProcessId] -= runTime;

                timeline.Add(new TimeSlotDto
                {
                    ProcessId = process.ProcessId,
                    StartTime = startTime,
                    EndTime = currentTime
                });

                // Enqueue all newly arrived processes FIRST (before re-enqueuing current)
                EnqueueNewArrivals(currentTime);

                // Then re-enqueue current process if still has burst left
                if (remainingBurst[process.ProcessId] > 0)
                    queue.Enqueue(process);
            }

            return BuildResult(timeline, processes);
        }

        private ScheduleResultDto RunSJF(List<ProcessInputDto> processes)
        {
            var remaining = processes.Select(p => new ProcessInputDto
            {
                ProcessId = p.ProcessId,
                ArrivalTime = p.ArrivalTime,
                BurstTime = p.BurstTime,
                Priority = p.Priority
            }).ToList();

            var timeline = new List<TimeSlotDto>();
            int currentTime = 0;
            var completed = new List<ProcessInputDto>();

            while (completed.Count < processes.Count)
            {
                // Among all arrived processes, pick shortest burst
                var available = remaining
                    .Where(p => p.ArrivalTime <= currentTime)
                    .OrderBy(p => p.BurstTime)
                    .ToList();

                if (!available.Any())
                {
                    // CPU idle — jump to next arrival
                    currentTime = remaining.Min(p => p.ArrivalTime);
                    continue;
                }

                var process = available.First();
                remaining.Remove(process);

                timeline.Add(new TimeSlotDto
                {
                    ProcessId = process.ProcessId,
                    StartTime = currentTime,
                    EndTime = currentTime + process.BurstTime
                });

                currentTime += process.BurstTime;
                completed.Add(process);
            }

            return BuildResult(timeline, processes);
        }

        private ScheduleResultDto RunPriority(List<ProcessInputDto> processes)
        {
            var remaining = processes.Select(p => new ProcessInputDto
            {
                ProcessId = p.ProcessId,
                ArrivalTime = p.ArrivalTime,
                BurstTime = p.BurstTime,
                Priority = p.Priority
            }).ToList();

            var timeline = new List<TimeSlotDto>();
            int currentTime = 0;
            var completed = new List<ProcessInputDto>();

            while (completed.Count < processes.Count)
            {
                // Among all arrived processes, pick highest priority (lowest number = highest priority)
                var available = remaining
                    .Where(p => p.ArrivalTime <= currentTime)
                    .OrderBy(p => p.Priority)
                    .ToList();

                if (!available.Any())
                {
                    currentTime = remaining.Min(p => p.ArrivalTime);
                    continue;
                }

                var process = available.First();
                remaining.Remove(process);

                timeline.Add(new TimeSlotDto
                {
                    ProcessId = process.ProcessId,
                    StartTime = currentTime,
                    EndTime = currentTime + process.BurstTime
                });

                currentTime += process.BurstTime;
                completed.Add(process);
            }

            return BuildResult(timeline, processes);
        }



        private ScheduleResultDto BuildResult(List<TimeSlotDto> timeline, List<ProcessInputDto> processes)
        {
            var stats = new List<ProcessStatsDto>();

            foreach (var process in processes)
            {
                // Last slot where this process ran = completion time
                int completionTime = timeline
                    .Where(t => t.ProcessId == process.ProcessId)
                    .Max(t => t.EndTime);

                int turnaroundTime = completionTime - process.ArrivalTime;
                int waitingTime = turnaroundTime - process.BurstTime;

                stats.Add(new ProcessStatsDto
                {
                    ProcessId = process.ProcessId,
                    CompletionTime = completionTime,
                    TurnaroundTime = turnaroundTime,
                    WaitingTime = waitingTime
                });
            }

            int totalTime = timeline.Max(t => t.EndTime) - timeline.Min(t => t.StartTime);

            return new ScheduleResultDto
            {
                Timeline = timeline,
                ProcessStats = stats,
                AverageWaitingTime = stats.Average(s => s.WaitingTime),
                AverageTurnaroundTime = stats.Average(s => s.TurnaroundTime),
                CpuUtilization = (double)processes.Sum(p => p.BurstTime) / totalTime * 100
            };
        }

        private ScheduleResultDto RunSRTF(List<ProcessInputDto> processes)
        {
            var remainingBurst = processes.ToDictionary(p => p.ProcessId, p => p.BurstTime);
            var timeline = new List<TimeSlotDto>();
            int currentTime = 0;
            int completed = 0;
            string? currentProcess = null;
            int slotStart = 0;

            int totalTime = processes.Sum(p => p.BurstTime) +
                            (processes.Min(p => p.ArrivalTime));

            while (completed < processes.Count)
            {
                // Among all arrived processes with remaining burst, pick shortest
                var available = processes
                    .Where(p => p.ArrivalTime <= currentTime && remainingBurst[p.ProcessId] > 0)
                    .OrderBy(p => remainingBurst[p.ProcessId])
                    .FirstOrDefault();

                if (available == null)
                {
                    currentTime++;
                    continue;
                }

                // If a different process should run now, close the current slot
                if (currentProcess != available.ProcessId)
                {
                    if (currentProcess != null)
                    {
                        timeline.Add(new TimeSlotDto
                        {
                            ProcessId = currentProcess,
                            StartTime = slotStart,
                            EndTime = currentTime
                        });
                    }
                    currentProcess = available.ProcessId;
                    slotStart = currentTime;
                }

                remainingBurst[available.ProcessId]--;
                currentTime++;

                if (remainingBurst[available.ProcessId] == 0)
                {
                    timeline.Add(new TimeSlotDto
                    {
                        ProcessId = currentProcess!,
                        StartTime = slotStart,
                        EndTime = currentTime
                    });
                    currentProcess = null;
                    completed++;
                }
            }

            return BuildResult(timeline, processes);
        }

        private ScheduleResultDto RunPriorityPreemptive(List<ProcessInputDto> processes)
        {
            var remainingBurst = processes.ToDictionary(p => p.ProcessId, p => p.BurstTime);
            var timeline = new List<TimeSlotDto>();
            int currentTime = 0;
            int completed = 0;
            string? currentProcess = null;
            int slotStart = 0;

            while (completed < processes.Count)
            {
                // Among all arrived processes with remaining burst, pick highest priority (lowest number)
                var available = processes
                    .Where(p => p.ArrivalTime <= currentTime && remainingBurst[p.ProcessId] > 0)
                    .OrderBy(p => p.Priority)
                    .FirstOrDefault();

                if (available == null)
                {
                    currentTime++;
                    continue;
                }

                // If a different process should run now, close the current slot
                if (currentProcess != available.ProcessId)
                {
                    if (currentProcess != null)
                    {
                        timeline.Add(new TimeSlotDto
                        {
                            ProcessId = currentProcess,
                            StartTime = slotStart,
                            EndTime = currentTime
                        });
                    }
                    currentProcess = available.ProcessId;
                    slotStart = currentTime;
                }

                remainingBurst[available.ProcessId]--;
                currentTime++;

                if (remainingBurst[available.ProcessId] == 0)
                {
                    timeline.Add(new TimeSlotDto
                    {
                        ProcessId = currentProcess!,
                        StartTime = slotStart,
                        EndTime = currentTime
                    });
                    currentProcess = null;
                    completed++;
                }
            }

            return BuildResult(timeline, processes);
        }
    }
}
