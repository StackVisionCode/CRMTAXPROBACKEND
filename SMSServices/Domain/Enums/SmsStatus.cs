namespace SMSServices.Domain.Enums;
public enum SmsStatus
{
    // Estados de mensajes salientes
    Queued,
    Sending,
    Sent,
    Delivered,
    Undelivered,
    Failed,
    
    // Estados de mensajes entrantes
    Received,
    
    // Estados generales
    Unknown
}