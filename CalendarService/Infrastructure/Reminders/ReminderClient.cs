using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.DTOs.Reminders; // <-- DTO compartido

namespace Infrastructure.Reminders;

public sealed class ReminderClient : IReminderClient
{
    private readonly HttpClient _http;
    private readonly ReminderClientOptions _opt;
    private readonly ILogger<ReminderClient> _log;

    public ReminderClient(HttpClient http, IOptions<ReminderClientOptions> opt, ILogger<ReminderClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;

        // Asegurar BaseAddress si no vino desde DI
        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opt.BaseUrl))
            _http.BaseAddress = new Uri(_opt.BaseUrl);

        // Header base opcional (útil p/ tracing)
        if (!string.IsNullOrWhiteSpace(_opt.DefaultCorrelationId))
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", _opt.DefaultCorrelationId);

        if (!string.IsNullOrWhiteSpace(_opt.UserAgent))
            _http.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse(_opt.UserAgent));
    }

    // Compat con tu handler actual (sin EventStartUtc)
    public Task ScheduleForEvent(Guid eventId, int[] daysBefore, CancellationToken ct = default)
    {
        _log.LogWarning("ScheduleForEvent(eventId, daysBefore) sin EventStartUtc. Considera usar la sobrecarga completa.");
        return ScheduleForEvent(eventId, DateTimeOffset.UtcNow, daysBefore, ct: ct);
    }

    // Completa
    public async Task ScheduleForEvent(
        Guid eventId,
        DateTimeOffset eventStartUtc,
        int[] daysBefore,
        TimeSpan? remindAtTime = null,
        string? message = null,
        string channel = "email",
        string? userId = null,
        CancellationToken ct = default
    )
    {
        if (daysBefore is null || daysBefore.Length == 0)
            throw new ArgumentException("daysBefore no puede ser null ni vacío.", nameof(daysBefore));

        var pathTemplate = _opt.EventsPathTemplate ?? "/reminders/api/reminders/events/{eventId}";
        var path = pathTemplate.Replace("{eventId}", eventId.ToString());

        // Normaliza: si no hay BaseAddress, path debe ser URL absoluta
        if (_http.BaseAddress is null && !path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_opt.BaseUrl))
                throw new InvalidOperationException("No hay BaseAddress ni BaseUrl configurado para ReminderClient.");
            path = $"{_opt.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        var body = new ScheduleEventReminderRequest
        {
            DaysBefore   = daysBefore,
            EventStartUtc= eventStartUtc,
            RemindAtTime = remindAtTime ?? _opt.DefaultRemindAtTime,
            Message      = message,
            UserId       = userId,
            Channel      = channel
        };

        var res = await _http.PostAsJsonAsync(path, body, ct);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(ct);
            _log.LogError("ScheduleForEvent fallo. Status={StatusCode} Path={Path} Body={Body}",
                          (int)res.StatusCode, path, content);
            res.EnsureSuccessStatusCode(); // lanzará con el status code real
        }
    }
}
