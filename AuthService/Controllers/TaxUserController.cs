using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Commands.InvitationCommands;
using AuthService.DTOs.InvitationDTOs;
using AuthService.DTOs.RoleDTOs;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using Infraestructure.Queries.InvitationsQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.UserQueries;
using Queries.UserRoleQueries;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxUserController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TaxUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Crear Usuario Admin o Preparador
        [HttpPost("CreateCompany")]
        public async Task<ActionResult<ApiResponse<bool>>> CreateCompany(
            [FromBody] NewCompanyDTO companyDto,
            [FromHeader(Name = "Origin")] string origin
        )
        {
            // Mapeas el DTO al Command (usando AutoMapper)
            var command = new CreateTaxCompanyCommands(companyDto, origin);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create company" });
            return Ok(result);
        }

        /// <summary>
        /// Envía una invitación a un email para unirse como TaxUser
        /// </summary>
        [HttpPost("invite")]
        public async Task<ActionResult<ApiResponse<InvitationDTO>>> SendInvitation(
            [FromBody] NewInvitationDTO invitation,
            [FromHeader(Name = "Origin")] string origin = ""
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var command = new SendUserInvitationCommand(
                invitation,
                userId,
                origin,
                ipAddress,
                userAgent
            );
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Registra un UserCompany usando un token de invitación
        /// </summary>
        [HttpPost("register-by-invitation")]
        public async Task<ActionResult<ApiResponse<bool>>> RegisterByInvitation(
            [FromBody] RegisterByInvitationDTO registration,
            [FromHeader(Name = "Origin")] string origin = ""
        )
        {
            var command = new RegisterUserByInvitationCommand(registration, origin);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Valida un token de invitación y retorna información de la company
        /// </summary>
        [HttpGet("validate-invitation/{token}")]
        public async Task<ActionResult<ApiResponse<InvitationValidationDTO>>> ValidateInvitation(
            string token
        )
        {
            var command = new ValidateInvitationCommand(token);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Obtiene todas las invitaciones de la company del usuario actual
        /// </summary>
        [HttpGet("invitations")]
        public async Task<
            ActionResult<ApiResponse<PagedResult<InvitationDTO>>>
        > GetCompanyInvitations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] InvitationStatus? status = null,
            [FromQuery] string? email = null,
            [FromQuery] Guid? invitedByUserId = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] bool includeExpired = true
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var companyIdRaw = User.FindFirst("companyId")?.Value;
            if (!Guid.TryParse(companyIdRaw, out var companyId))
                return Unauthorized();

            var query = new GetCompanyInvitationsQuery(
                companyId,
                page,
                pageSize,
                status,
                email,
                invitedByUserId,
                dateFrom,
                dateTo,
                includeExpired
            );

            var result = await _mediator.Send(query);
            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene una invitación específica por ID
        /// </summary>
        [HttpGet("invitations/{invitationId:guid}")]
        public async Task<ActionResult<ApiResponse<InvitationDTO>>> GetInvitationById(
            Guid invitationId
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var query = new GetInvitationByIdQuery(invitationId, userId);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene invitación por token (para validación pública)
        /// </summary>
        [HttpGet("invitations/by-token/{token}")]
        public async Task<ActionResult<ApiResponse<InvitationDTO>>> GetInvitationByToken(
            string token
        )
        {
            var query = new GetInvitationByTokenQuery(token);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancela una invitación específica
        /// </summary>
        [HttpPut("invitations/{invitationId:guid}/cancel")]
        public async Task<ActionResult<ApiResponse<bool>>> CancelInvitation(
            Guid invitationId,
            [FromBody] CancelInvitationDTO cancelRequest
        )
        {
            if (cancelRequest.InvitationId != invitationId)
                return BadRequest("Invitation ID mismatch");

            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var command = new CancelInvitationCommand(cancelRequest, userId);
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Cancela múltiples invitaciones
        /// </summary>
        [HttpPut("invitations/cancel-bulk")]
        public async Task<ActionResult<ApiResponse<int>>> CancelBulkInvitations(
            [FromBody] CancelBulkInvitationsDTO cancelRequest
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var command = new CancelBulkInvitationsCommand(cancelRequest, userId);
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene estadísticas de invitaciones de la company
        /// </summary>
        [HttpGet("invitations/stats")]
        public async Task<ActionResult<ApiResponse<InvitationStatsDTO>>> GetInvitationStats(
            [FromQuery] int daysBack = 30
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var companyIdRaw = User.FindFirst("companyId")?.Value;
            if (!Guid.TryParse(companyIdRaw, out var companyId))
                return Unauthorized();

            var query = new GetInvitationStatsQuery(companyId, daysBack);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Verifica si la company puede enviar más invitaciones
        /// </summary>
        [HttpGet("invitations/can-send-more")]
        public async Task<
            ActionResult<ApiResponse<InvitationLimitCheckDTO>>
        > CanSendMoreInvitations()
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized();

            var companyIdRaw = User.FindFirst("companyId")?.Value;
            if (!Guid.TryParse(companyIdRaw, out var companyId))
                return Unauthorized();

            var query = new CanSendMoreInvitationsQuery(companyId);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Marca invitaciones expiradas (job manual - solo Developer)
        /// </summary>
        [HttpPost("invitations/mark-expired")]
        public async Task<ActionResult<ApiResponse<int>>> MarkExpiredInvitations()
        {
            var command = new MarkExpiredInvitationsCommand();
            var result = await _mediator.Send(command);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene todas las invitaciones del sistema (solo Developer)
        /// </summary>
        [HttpGet("admin/invitations")]
        public async Task<ActionResult<ApiResponse<PagedResult<InvitationDTO>>>> GetAllInvitations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] Guid? companyId = null,
            [FromQuery] InvitationStatus? status = null,
            [FromQuery] string? email = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] bool includeExpired = true
        )
        {
            var query = new GetAllInvitationsQuery(
                page,
                pageSize,
                companyId,
                status,
                email,
                dateFrom,
                dateTo,
                includeExpired
            );
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Obtiene estadísticas globales de invitaciones (solo Developer)
        /// </summary>
        [HttpGet("admin/invitations/global-stats")]
        public async Task<
            ActionResult<ApiResponse<List<InvitationStatsDTO>>>
        > GetGlobalInvitationStats([FromQuery] int daysBack = 30)
        {
            var query = new GetGlobalInvitationStatsQuery(daysBack);
            var result = await _mediator.Send(query);

            return result.Success ?? false ? Ok(result) : BadRequest(result);
        }

        // Actualizar Usuario de Compañia
        [HttpPut("UpdateUser")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUser(
            [FromBody] UpdateUserDTO userDto
        )
        {
            var command = new UpdateTaxUserCommands(userDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update user" });

            return Ok(result);
        }

        // Actualizar Usuario de Admin o Prpeparador
        [HttpPut("UpdateCompany")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateCompany(
            [FromBody] UpdateCompanyDTO companyDto
        )
        {
            var command = new UpdateTaxCompanyCommands(companyDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update company" });

            return Ok(result);
        }

        // Actualizar Usuario de Compañia
        [HttpPut("UpdateProfile")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile(
            [FromBody] UpdateUserDTO userDto
        )
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized(new ApiResponse<bool>(false, "Invalid session"));

            // Asegurar que el usuario solo pueda actualizar su propio perfil
            userDto.Id = userId;

            var command = new UpdateTaxUserCommands(userDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update profile" });

            return Ok(result);
        }

        // Actualizar Usuario Admin O Preparador
        [HttpPut("UpdateCompanyProfile")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateCompanyProfile(
            [FromBody] UpdateCompanyDTO companyDto
        )
        {
            var userIdRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var companyIdRaw = User.FindFirst("companyId")?.Value;

            if (
                !Guid.TryParse(userIdRaw, out var userId)
                || !Guid.TryParse(companyIdRaw, out var companyId)
            )
                return Unauthorized(new ApiResponse<bool>(false, "Invalid session"));

            var isOwnerRaw = User.FindFirst("isOwner")?.Value;
            if (!bool.TryParse(isOwnerRaw, out var isOwner) || !isOwner)
                return Forbid("Only company owners can update company profile");

            companyDto.Id = companyId;

            var command = new UpdateTaxCompanyCommands(companyDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update company profile" });

            return Ok(result);
        }

        /// Elimina un usuario de la compañía (solo Users regulares, no Owners)
        [HttpDelete("DeleteUser/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid userId)
        {
            var command = new DeleteTaxUserCommands(userId);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        /// Habilita un usuario desactivado de la compañía
        [HttpPut("EnableUser/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> EnableUser(Guid userId)
        {
            var command = new EnableUserCommand(userId);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        /// Deshabilita un usuario de la compañía (revoca sesiones activas)
        [HttpPut("DisableUser/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DisableUser(Guid userId)
        {
            var command = new DisableUserCommand(userId);
            var result = await _mediator.Send(command);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        // Obtener Perfil de Usuario de Compañia
        [HttpGet("Profile")]
        public async Task<ActionResult<ApiResponse<UserProfileDTO>>> GetProfile()
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized(new ApiResponse<UserProfileDTO>(false, "Invalid session"));

            var command = new GetTaxUserProfileQuery(userId);
            var result = await _mediator.Send(command);

            if (result.Success != true || result.Data == null)
                return BadRequest(result);

            // Enriquecer con datos del JWT cuando estén vacíos en BD
            var profileDto = result.Data;

            // Si name/lastName están vacíos, usar los del JWT
            if (string.IsNullOrWhiteSpace(profileDto.Name))
            {
                profileDto.Name = User.FindFirst(ClaimTypes.GivenName)?.Value;
            }

            if (string.IsNullOrWhiteSpace(profileDto.LastName))
            {
                profileDto.LastName = User.FindFirst(ClaimTypes.Surname)?.Value;
            }

            if (string.IsNullOrWhiteSpace(profileDto.CompanyName))
            {
                profileDto.CompanyName = User.FindFirst("companyName")?.Value;
            }

            if (string.IsNullOrWhiteSpace(profileDto.CompanyDomain))
            {
                profileDto.CompanyDomain = User.FindFirst("companyDomain")?.Value;
            }

            if (bool.TryParse(User.FindFirst("isCompany")?.Value, out var isCompany))
            {
                profileDto.CompanyIsIndividual = !isCompany;
            }

            var enrichedResponse = new ApiResponse<UserProfileDTO>(true, "Ok", profileDto);
            return Ok(enrichedResponse);
        }

        [HttpGet("GetMyRoles")]
        public async Task<ActionResult<ApiResponse<List<RoleDTO>>>> GetMyRoles()
        {
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized(new ApiResponse<List<RoleDTO>>(false, "Invalid session"));

            var query = new GetRolesByUserIdQuery(userId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetByUserId")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var command = new GetTaxUserByIdQuery(id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }
    }
}
