using SMSServices.Application.DTO;
using SMSServices.Application.Models;

namespace SMSServices.Infrastructure.Services;

public interface ISmsService
{
   Task<ApiResponse<SmsResponseDto>> SendSmsAsync(SmsRequestDto request);
    Task<ApiResponse<List<SmsResponseDto>>> GetMessageHistoryAsync(string? phoneNumber = null, int skip = 0, int take = 50);
    Task<ApiResponse<SmsResponseDto>> GetMessageByIdAsync(Guid id);
    Task<ApiResponse<IncomingSmsDto>> ProcessIncomingSmsAsync(IFormCollection form);
    Task<ApiResponse<SmsResponseDto>> SendSmsWithTemplateAsync(string to, string templateName, Dictionary<string, string> parameters);
    Task<ApiResponse<List<SmsResponseDto>>> GetMessagesByStatusAsync(string status, int skip = 0, int take = 50);
    Task<bool> UpdateMessageStatusAsync(string messageSid, string status);
}