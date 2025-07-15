using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiroTalkService.Libs;

namespace MiroTalkService.Controllers;
[ApiController]
[Route("api/[controller]")]
public class MiroTalkController : ControllerBase
{
    private readonly MiroTalkClient _client;

    public MiroTalkController(MiroTalkClient client)
    {
        _client = client;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateMeeting()
    {
        var url = await _client.CreateMeetingAsync();
        return Ok(new { url });
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinMeeting([FromBody] JoinRequest request)
    {
        var url = await _client.JoinMeetingAsync(request.Room, request.Name);
        return Ok(new { url });
    }

    [HttpPost("token")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenRequest request)
    {
        var token = await _client.GenerateTokenAsync(request.Username, request.Password);
        return Ok(new { token });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _client.GetStatsAsync();
        return Ok(stats);
    }
}

public record JoinRequest(string Room, string Name);
public record TokenRequest(string Username, string Password);
