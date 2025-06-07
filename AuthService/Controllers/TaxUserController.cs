using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.UserQueries;

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

        [HttpPost("Create")]
        public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] NewUserDTO userDto, [FromHeader(Name = "Origin")] string origin)
        {
            // Mapeas el DTO al Command (usando AutoMapper)
            var command = new CreateTaxUserCommands(userDto, origin);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to create user" });
            return Ok(result);
        }

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

        // Endpoint adicional para actualizar el perfil del usuario autenticado
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

        // Endpoint adicional para actualizar el perfil del usuario autenticado
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

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteTaxUserCommands(id);
            var result = await _mediator.Send(command);
            if (result == null)
                return BadRequest(new { message = "Failed to delete user" });
            return Ok(result);
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<ApiResponse<UserGetDTO[]>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllUserQuery());

            if (result.Success == false)
                return BadRequest(new { result });

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

            // AGREGAR: Si FullName está vacío, usar el del JWT
            if (string.IsNullOrWhiteSpace(profileDto.FullName))
            {
                profileDto.FullName = User.FindFirst("fullName")?.Value;
            }

            // OPCIONAL: También puedes enriquecer otros campos si están vacíos
            if (string.IsNullOrWhiteSpace(profileDto.CompanyName))
            {
                profileDto.CompanyName = User.FindFirst("companyName")?.Value;
            }

            if (string.IsNullOrWhiteSpace(profileDto.CompanyBrand))
            {
                profileDto.CompanyBrand = User.FindFirst("companyBrand")?.Value;
            }

            // Crear respuesta enriquecida
            var enrichedResponse = new ApiResponse<UserProfileDTO>(true, "Ok", profileDto);

            return Ok(enrichedResponse);
        }
    }
}
