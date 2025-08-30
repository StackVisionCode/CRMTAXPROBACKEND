using Microsoft.AspNetCore.Mvc;
using SMSServices.Application.DTO;
using SMSServices.Application.Models;
using SMSServices.Infrastructure.Services;
namespace SMSServices.Controllers;
[ApiController]
[Route("api/[controller]")]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(ISmsService smsService, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Envía un SMS usando Twilio (ENDPOINT QUE LLAMAS TÚ)
    /// </summary>
    /// <param name="request">Datos del SMS a enviar</param>
    /// <returns>Resultado del envío</returns>
    [HttpPost("EnviarSms")]
    public async Task<ActionResult<ApiResponse<SmsResponseDto>>> EnviarSms([FromBody] SmsRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<SmsResponseDto>
            {
                Success = false,
                Message = "Datos de entrada inválidos",
                Errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var result = await _smsService.SendSmsAsync(request);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Webhook para recibir SMS entrantes de Twilio (ENDPOINT QUE LLAMA TWILIO)
    /// </summary>
    /// <returns>Respuesta TwiML</returns>
    [HttpPost("RecibirSms")]
    public async Task<ActionResult<string>> RecibirSms()
    {
        try
        {
            var result = await  _smsService.ProcessIncomingSmsAsync(Request.Form);
            // var result = _smsService.SendSmsAsync(request.Form);
            
            if (result.Success && result.Data != null)
            {
                // Puedes procesar el mensaje aquí
                // Por ejemplo, responder automáticamente
                var responseMessage = ProcessIncomingMessage(result.Data);

                // Respuesta TwiML para responder al SMS (opcional)
                var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Response>
                    <Message>{responseMessage}</Message>
                </Response>";

                return Content(twiml, "application/xml");
            }
            
            // Respuesta vacía si no hay respuesta automática
            return Content(@"<?xml version=""1.0"" encoding=""UTF-8""?><Response></Response>", "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en webhook de SMS");
            return Content(@"<?xml version=""1.0"" encoding=""UTF-8""?><Response></Response>", "application/xml");
        }
    }

    /// <summary>
    /// Obtiene el historial de mensajes con paginación
    /// </summary>
    /// <param name="phoneNumber">Número de teléfono opcional para filtrar</param>
    /// <param name="skip">Número de registros a saltar (paginación)</param>
    /// <param name="take">Número de registros a tomar (máximo 100)</param>
    /// <returns>Lista paginada de mensajes</returns>
    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<SmsResponseDto>>>> GetMessageHistory(
        [FromQuery] string? phoneNumber = null, 
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (take > 100)
        {
            return BadRequest(new ApiResponse<List<SmsResponseDto>>
            {
                Success = false,
                Message = "El máximo de registros por consulta es 100"
            });
        }

        var result = await _smsService.GetMessageHistoryAsync(phoneNumber, skip, take);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Obtiene un mensaje específico por ID
    /// </summary>
    /// <param name="id">ID del mensaje</param>
    /// <returns>Datos del mensaje</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SmsResponseDto>>> GetMessage(Guid id)
    {
        var result = await _smsService.GetMessageByIdAsync(id);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return result.Data == null ? NotFound(result) : BadRequest(result);
    }

    /// <summary>
    /// Envía SMS usando una plantilla predefinida
    /// </summary>
    /// <param name="request">Datos para envío con plantilla</param>
    /// <returns>Resultado del envío</returns>
    // [HttpPost("EnviarConPlantilla")]
    // public async Task<ActionResult<ApiResponse<SmsResponseDto>>> EnviarSmsConPlantilla(
    //     [FromBody] SmsTemplateRequestDto request)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return BadRequest(new ApiResponse<SmsResponseDto>
    //         {
    //             Success = false,
    //             Message = "Datos de entrada inválidos",
    //             Errors = ModelState.Values
    //                 .SelectMany(v => v.Errors)
    //                 .Select(e => e.ErrorMessage)
    //                 .ToList()
    //         });
    //     }

    //     var result = await _smsService.SendSmsWithTemplateAsync(
    //         request.To, request.TemplateName, request.Parameters);
        
    //     if (result.Success)
    //     {
    //         return Ok(result);
    //     }
        
    //     return BadRequest(result);
    // }

    /// <summary>
    /// Obtiene mensajes filtrados por estado
    /// </summary>
    /// <param name="status">Estado del mensaje (sent, delivered, failed, etc.)</param>
    /// <param name="skip">Registros a saltar</param>
    /// <param name="take">Registros a tomar</param>
    /// <returns>Lista de mensajes</returns>
    [HttpGet("by-status/{status}")]
    public async Task<ActionResult<ApiResponse<List<SmsResponseDto>>>> GetMessagesByStatus(
        string status, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 50)
    {
        var result = await _smsService.GetMessagesByStatusAsync(status, skip, take);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    /// <summary>
    /// Endpoint de salud para verificar la configuración de Twilio
    /// </summary>
    /// <returns>Estado del servicio</returns>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Twilio SMS Service",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Procesa mensajes entrantes y genera respuestas automáticas
    /// </summary>
    /// <param name="incomingSms">SMS entrante</param>
    /// <returns>Mensaje de respuesta</returns>
    private string ProcessIncomingMessage(IncomingSmsDto incomingSms)
    {
        // Lógica para procesar el mensaje entrante
        // Puedes implementar comandos, respuestas automáticas, etc.
        
        var body = incomingSms.Body.ToLower().Trim();
        
        return body switch
        {
            "hola" or "hello" => "¡Hola! Gracias por contactarnos. ¿En qué podemos ayudarte?",
            "help" or "ayuda" => "Comandos disponibles: HOLA, AYUDA, INFO, STOP",
            "info" => "Este es un servicio automatizado de SMS. Para más información visita nuestro sitio web.",
            "stop" => "Has sido removido de nuestras comunicaciones SMS.",
            _ => "Gracias por tu mensaje. Te responderemos pronto."
        };
    }
}