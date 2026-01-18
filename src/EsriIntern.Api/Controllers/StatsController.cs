using EsriIntern.Api.Data;
using EsriIntern.Api.Dtos;
using EsriIntern.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsriIntern.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Produces("application/json")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly DemographicsRefresher _refresher;
    private readonly ILogger<StatsController> _logger;

    public StatsController(AppDbContext db, DemographicsRefresher refresher, ILogger<StatsController> logger)
    {
        _db = db;
        _refresher = refresher;
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new HealthResponseDto
        {
            Status = "ok",
            Utc = DateTime.UtcNow
        });
    }

    [HttpGet("states")]
    [ProducesResponseType(typeof(List<StatePopulationResponseDto>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<List<StatePopulationResponseDto>>> GetStates(
        [FromQuery] string? stateName,
        CancellationToken ct)
    {
        var query = _db.StatePopulations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(stateName))
        {
            var trimmed = stateName.Trim();

            if (trimmed.Length < 2)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Status = StatusCodes.Status400BadRequest,
                    Message = "State name filter must be at least 2 characters long."
                });
            }

            var lowered = trimmed.ToLowerInvariant();
            query = query.Where(x => x.StateName.ToLower()!.Contains(lowered));

            _logger.LogDebug("Filtering states by name: {StateName}", trimmed);
        }

        var rows = await query
            .OrderBy(x => x.StateName)
            .Select(x => new StatePopulationResponseDto
            {
                StateName = x.StateName,
                Population = x.Population,
                RetrievedAtUtc = x.RetrievedAtUtc
            })
            .ToListAsync(ct);

        _logger.LogInformation("Retrieved {Count} states", rows.Count);
        return Ok(rows);
    }

    [HttpGet("states/{stateName}")]
    [ProducesResponseType(typeof(StatePopulationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 60)]
    public async Task<ActionResult<StatePopulationResponseDto>> GetState(
        [FromRoute] string stateName,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return BadRequest(new ErrorResponseDto
            {
                Status = StatusCodes.Status400BadRequest,
                Message = "State name cannot be empty."
            });
        }

        var row = await _db.StatePopulations
            .Where(x => x.StateName == stateName)
            .Select(x => new StatePopulationResponseDto
            {
                StateName = x.StateName,
                Population = x.Population,
                RetrievedAtUtc = x.RetrievedAtUtc
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
        {
            _logger.LogWarning("State not found: {StateName}", stateName);
            return NotFound(new ErrorResponseDto
            {
                Status = StatusCodes.Status404NotFound,
                Message = $"State '{stateName}' not found. The data might not have been refreshed yet."
            });
        }

        return Ok(row);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RefreshResponseDto>> Refresh(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Manual refresh triggered");
            var at = await _refresher.RefreshAsync(ct);

            var statesCount = await _db.StatePopulations.CountAsync(ct);

            return Ok(new RefreshResponseDto
            {
                RefreshedAtUtc = at,
                StatesCount = statesCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual refresh failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponseDto
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "Failed to refresh demographics data. Please check logs for details.",
                Details = HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                    ? ex.ToString()
                    : null
            });
        }
    }
}
