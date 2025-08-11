using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.RoleDTOs;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CompanyQueries;
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
            var idRaw =
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(idRaw, out var userId))
                return Unauthorized(new ApiResponse<bool>(false, "Invalid session"));

            // Asegurar que el usuario solo pueda actualizar su propio perfil
            companyDto.Id = userId;

            var command = new UpdateTaxCompanyCommands(companyDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update profile" });

            return Ok(result);
        }

        // Eliminar Usuario de Compañia
        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteTaxUserCommands(id);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to delete user" });
            return Ok(result);
        }

        // Obtener todas las Compañias
        [HttpGet("GetAll")]
        public async Task<ActionResult<ApiResponse<UserGetDTO[]>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllUserQuery());

            if (result.Success == false)
                return BadRequest(new { result });

            return Ok(result);
        }

        // Obtener compañia por Id
        [HttpGet("GetByUserId")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var command = new GetTaxUserByIdQuery(id);
            var result = await _mediator.Send(command);
            if (result.Success == false)
                return BadRequest(new { result });

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

        [HttpGet("GetCompanyUsers")]
        public async Task<ActionResult<ApiResponse<List<UserGetDTO>>>> GetCompanyUsers(
            Guid companyId
        )
        {
            var query = new GetUsersByCompanyIdQuery(companyId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetMyCompanyUsers")]
        public async Task<ActionResult<ApiResponse<List<UserGetDTO>>>> GetMyCompanyUsers()
        {
            // Obtener CompanyId del JWT
            var companyIdRaw = User.FindFirst("companyId")?.Value;
            if (!Guid.TryParse(companyIdRaw, out var companyId))
            {
                return Unauthorized(
                    new ApiResponse<List<UserGetDTO>>(false, "Invalid company session")
                );
            }

            var query = new GetMyCompanyUsersQuery(companyId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
