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

    [HttpGet("{companyId:Guid}")]
    public async Task<ActionResult<ApiResponse<EmailStatistics>>> GetStatistics(
        Guid companyId,
        [FromQuery] Guid? taxUserId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null
    )
    {
        try
        {
            var stats = await _statisticsService.GetStatisticsAsync(
                companyId,
                taxUserId,
                fromDate,
                toDate
            );
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

    [HttpGet("{companyId:Guid}/daily")]
    public async Task<ActionResult<ApiResponse<DailyEmailStats[]>>> GetDailyStats(
        Guid companyId,
        [FromQuery] Guid? taxUserId = null,
        [FromQuery] DateTime fromDate = default,
        [FromQuery] DateTime toDate = default
    )
    {
        try
        {
            if (fromDate == default || toDate == default)
            {
                var errorResponse = new ApiResponse<DailyEmailStats[]>(
                    false,
                    "FromDate and ToDate are required",
                    null
                );
                return BadRequest(errorResponse);
            }

            if (fromDate > toDate)
            {
                var errorResponse = new ApiResponse<DailyEmailStats[]>(
                    false,
                    "FromDate cannot be greater than ToDate",
                    null
                );
                return BadRequest(errorResponse);
            }

            var stats = await _statisticsService.GetDailyStatsAsync(
                companyId,
                taxUserId,
                fromDate,
                toDate
            );
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
