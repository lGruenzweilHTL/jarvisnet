using System.Text.Json;
using AssistantCore.Workers;
using AssistantCore.Workers.Dto;
using Microsoft.AspNetCore.Mvc;

namespace AssistantCore.Controllers;

[ApiController]
[Route("worker")]
public class WorkerController(WorkerRegistry registry) : ControllerBase
{
    [HttpPost("register")]
    public IActionResult RegisterWorker([FromBody] WorkerRegisterRequest request)
    {
        var typeStr = request.WorkerType.Split(':')[0];
        var speciality = request.WorkerType.Contains(':') ? request.WorkerType.Split(':')[1] : null;
        if (!Enum.TryParse<WorkerType>(typeStr, ignoreCase: true, out var type))
            return BadRequest("Invalid worker type");
        
        var workerId = Guid.NewGuid().ToString();
        var descriptor = new WorkerDescriptor
        {
            Type = type,
            Endpoint = new Uri(request.Endpoint),
            WorkerId = workerId,
            Capabilities = new WorkerCapabilities
            {
                // TODO
            }
        };
        
        registry.Register(descriptor);
        
        var result = new WorkerRegisterResult
        {
            Accepted = true,
            WorkerId = workerId
        };
        return Ok(result);
    }

    [HttpPost("heartbeat")]
    public IActionResult Heartbeat([FromBody] WorkerHeartbeatRequest request)
    {
        registry.Heartbeat(request.WorkerId);
        return Ok();
    }

    [HttpGet("")]
    public IActionResult GetAllWorkers()
    {
        return Ok(registry.GetAllWorkers());
    }
    
    [HttpGet("alive")]
    public IActionResult GetAliveWorkers()
    {
        return Ok(registry.GetAliveWorkers());
    }
    
    [HttpGet("{workerId}")]
    public IActionResult GetWorker(string workerId)
    {
        var worker = registry.GetWorker(workerId);
        if (worker == null)
            return NotFound("Worker not found");
        return Ok(worker);
    }
}