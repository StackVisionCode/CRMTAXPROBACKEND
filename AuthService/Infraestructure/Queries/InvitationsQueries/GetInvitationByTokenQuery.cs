using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener una invitación por token (para validación)
/// </summary>
public record GetInvitationByTokenQuery(string Token) : IRequest<ApiResponse<InvitationDTO>>;
