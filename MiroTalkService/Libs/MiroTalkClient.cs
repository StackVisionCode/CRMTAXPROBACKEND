using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MiroTalkService.LibsModels;

namespace MiroTalkService.Libs;

public class MiroTalkClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public MiroTalkClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;

        _httpClient.BaseAddress = new Uri(_config["MiroTalk:BaseUrl"]!);
        _httpClient.DefaultRequestHeaders.Add("authorization", _config["MiroTalk:ApiKeySecret"]!);
    }

    public async Task<string> CreateMeetingAsync()
    {
        var response = await _httpClient.PostAsync("/api/v1/meeting", null);
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode==false || string.IsNullOrWhiteSpace(content) || content.TrimStart().StartsWith("<"))
        {
            throw new Exception($"[CreateMeeting] Invalid response:\n{content}");
        }

        try
        {
            var result = JsonSerializer.Deserialize<MiroTalkMeetingResponse>(content);
            return result?.meeting ?? throw new Exception("Campo 'meeting' no encontrado.");
        }
        catch (JsonException ex)
        {
            throw new Exception($"[CreateMeeting] Error al parsear JSON: {ex.Message}\nContenido:\n{content}");
        }
    }

    public async Task<string> JoinMeetingAsync(string room, string name)
    {
        var body = new
        {
            room,
            roomPassword = false,
            name,
            avatar = false,
            audio = false,
            video = false,
            screen = false,
            notify = false,
            duration = "unlimited"
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/join", body);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || content.TrimStart().StartsWith("<"))
        {
            throw new Exception($"[JoinMeeting] Invalid response:\n{content}");
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        return data!["join"];
    }

    public async Task<string> GenerateTokenAsync(string username, string password)
    {
        var body = new
        {
            username,
            password,
            presenter = true,
            expire = "1h"
        };

        var response = await _httpClient.PostAsJsonAsync("api/v1/token", body);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || content.TrimStart().StartsWith("<"))
        {
            throw new Exception($"[GenerateToken] Invalid response:\n{content}");
        }

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        return data!["token"];
    }

    public async Task<Dictionary<string, object>> GetStatsAsync()
    {
        var response = await _httpClient.GetAsync("api/v1/stats");
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || content.TrimStart().StartsWith("<"))
        {
            throw new Exception($"[GetStats] Invalid response:\n{content}");
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(content)!;
    }
}
