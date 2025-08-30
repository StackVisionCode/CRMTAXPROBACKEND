/// <summary>
/// Estados posibles de una invitación
/// </summary>
public enum InvitationStatus
{
    Pending = 1, // Enviada, esperando respuesta
    Accepted = 2, // Aceptada y usuario registrado
    Cancelled = 3, // Cancelada manualmente
    Expired = 4, // Expirada por tiempo
    Failed = 5, // Error en el envío/procesamiento
}
