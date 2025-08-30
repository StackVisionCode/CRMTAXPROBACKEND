
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using SMSServices.Infrastructure.Services;
using SMSServices.Application.Models;
using SMSServices.Infrastructure.Context;
using SMSServices.Application.DTO;
using SMSServices.Domain.Entities;

namespace SMSServices.Infrastructure.Repositories;



public class SmsService : ISmsService
{
    private readonly TwilioSettings _twilioSettings;
    private readonly ILogger<SmsService> _logger;
    private readonly SmsDbContext _dbContext;

    public SmsService(
        IOptions<TwilioSettings> twilioSettings, 
        ILogger<SmsService> logger,
        SmsDbContext dbContext)
    {
        _twilioSettings = twilioSettings.Value;
        _logger = logger;
        _dbContext = dbContext;
        
        // Inicializar Twilio
        TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);
    }

    public async Task<ApiResponse<SmsResponseDto>> SendSmsAsync(SmsRequestDto request)
    {
        //  using var transaction = await _dbContext.Database.BeginTransactionAsync();
        
        try
        {
            _logger.LogInformation("Enviando SMS a {To} con mensaje: {Message}", request.To, request.Message);

            // 1. Crear registro en BD antes de enviar (estado inicial)
            var smsEntity = new SmsMessage
            {
                From = _twilioSettings.PhoneNumber,
                To = request.To,
                Body = request.Message,
                Status = "queued",
                Direction = SmsDirection.Outbound,
                CreatedAt = DateTime.UtcNow
            };



            // _dbContext.SmsMessages.Add(smsEntity);
            // await _dbContext.SaveChangesAsync();

            // 2. Enviar SMS con Twilio
            var message = await MessageResource.CreateAsync(
                body: request.Message,
                from: new PhoneNumber(_twilioSettings.PhoneNumber),
                to: new PhoneNumber(request.To)
            );

            // 3. Actualizar registro con datos de Twilio
            smsEntity.MessageSid = message.Sid;
            smsEntity.Status = message.Status?.ToString() ?? "unknown";
            smsEntity.Price = Convert.ToDecimal(message.Price);
            smsEntity.PriceUnit = message.PriceUnit;
            smsEntity.NumSegments = message.NumSegments;
            smsEntity.UpdatedAt = DateTime.UtcNow;

            // await _dbContext.SaveChangesAsync();
            // await transaction.CommitAsync();

            var response = new SmsResponseDto
            {
                Id = smsEntity.Id,
                MessageSid = smsEntity.MessageSid,
                Status = smsEntity.Status,
                To = smsEntity.To,
                From = smsEntity.From,
                Body = smsEntity.Body,
                Direction = smsEntity.Direction,
                CreatedAt = smsEntity.CreatedAt,
                Price = smsEntity.Price,
                PriceUnit = smsEntity.PriceUnit,
                NumSegments = smsEntity.NumSegments
            };

            _logger.LogInformation("SMS enviado exitosamente. ID: {Id}, SID: {MessageSid}", 
                smsEntity.Id, message.Sid);

            return new ApiResponse<SmsResponseDto>
            {
                Success = true,
                Message = "SMS enviado y guardado exitosamente",
                Data = response
            };
        }
        catch (Exception ex)
        {
            // await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al enviar SMS a {To}", request.To);
            
            return new ApiResponse<SmsResponseDto>
            {
                Success = false,
                Message = "Error al enviar SMS",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<List<SmsResponseDto>>> GetMessageHistoryAsync(string? phoneNumber = null, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Obteniendo historial de mensajes. Teléfono: {Phone}, Límite: {Limit}", 
                phoneNumber ?? "todos", limit);

            var messages = await MessageResource.ReadAsync(
                to: string.IsNullOrEmpty(phoneNumber) ? null : new PhoneNumber(phoneNumber),
                limit: limit
            );

            var response = messages.Select(m => new SmsResponseDto
            {
                MessageSid = m.Sid,
                Status = m.Status.ToString(),
                To = m.To,
                From = m.From.ToString(),
                Body = m.Body,
                CreatedAt = m.DateCreated ?? DateTime.UtcNow
            }).ToList();

            return new ApiResponse<List<SmsResponseDto>>
            {
                Success = true,
                Message = $"Se encontraron {response.Count} mensajes",
                Data = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de mensajes");
            
            return new ApiResponse<List<SmsResponseDto>>
            {
                Success = false,
                Message = "Error al obtener historial",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public ApiResponse<IncomingSmsDto> ProcessIncomingSms(IFormCollection form)
    {
        try
        {
            var incomingSms = new IncomingSmsDto
            {
                MessageSid = form["MessageSid"].ToString(),
                From = form["From"].ToString(),
                To = form["To"].ToString(),
                Body = form["Body"].ToString(),
                NumMedia = form["NumMedia"].ToString(),
                DateReceived = DateTime.UtcNow
            };

            _logger.LogInformation("SMS recibido de {From}: {Body}", incomingSms.From, incomingSms.Body);

            // Aquí puedes agregar lógica adicional como:
            // - Guardar en base de datos
            // - Enviar a una cola de mensajes
            // - Procesar comandos específicos
            
            return new ApiResponse<IncomingSmsDto>
            {
                Success = true,
                Message = "SMS procesado exitosamente",
                Data = incomingSms
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar SMS entrante");
            
            return new ApiResponse<IncomingSmsDto>
            {
                Success = false,
                Message = "Error al procesar SMS",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public Task<ApiResponse<List<SmsResponseDto>>> GetMessageHistoryAsync(string? phoneNumber = null, int skip = 0, int take = 50)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<SmsResponseDto>> GetMessageByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<IncomingSmsDto>> ProcessIncomingSmsAsync(IFormCollection form)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<SmsResponseDto>> SendSmsWithTemplateAsync(string to, string templateName, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }

    public Task<ApiResponse<List<SmsResponseDto>>> GetMessagesByStatusAsync(string status, int skip = 0, int take = 50)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateMessageStatusAsync(string messageSid, string status)
    {
        throw new NotImplementedException();
    }
}