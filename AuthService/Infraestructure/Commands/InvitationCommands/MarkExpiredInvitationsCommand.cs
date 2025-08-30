using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

/// <summary>
/// Command para marcar invitaciones expiradas (job background)
/// </summary>
public record MarkExpiredInvitationsCommand() : IRequest<ApiResponse<int>>; // Retorna cantidad marcada como expirada
