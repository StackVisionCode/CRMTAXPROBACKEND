using Common;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailStatisticsController : ControllerBase
{
    private readonly IEmailStatisticsService _statisticsService;

    public EmailStatisticsController(IEmailStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("{userId:Guid}")]
    public async Task<ActionResult<ApiResponse<EmailStatistics>>> GetStatistics(
        Guid userId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null
    )
    {
        try
        {
            var stats = await _statisticsService.GetStatisticsAsync(userId, fromDate, toDate);
            var response = new ApiResponse<EmailStatistics>(
                true,
                "Statistics retrieved successfully",
                stats
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<EmailStatistics>(false, ex.Message, null);
            return BadRequest(response);
        }
    }

    [HttpGet("{userId:Guid}/daily")]
    public async Task<ActionResult<ApiResponse<DailyEmailStats[]>>> GetDailyStats(
        Guid userId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate
    )
    {
        try
        {
            if (fromDate > toDate)
            {
                var errorResponse = new ApiResponse<DailyEmailStats[]>(
                    false,
                    "FromDate cannot be greater than ToDate",
                    null
                );
                return BadRequest(errorResponse);
            }

            var stats = await _statisticsService.GetDailyStatsAsync(userId, fromDate, toDate);
            var response = new ApiResponse<DailyEmailStats[]>(
                true,
                "Daily statistics retrieved successfully",
                stats
            );
            return Ok(response);
        }
        catch (Exception ex)
        {
            var response = new ApiResponse<DailyEmailStats[]>(false, ex.Message, null);
            return BadRequest(response);
        }
    }
}
