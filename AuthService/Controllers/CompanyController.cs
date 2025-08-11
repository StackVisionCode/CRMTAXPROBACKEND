using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.DTOs.CompanyDTOs;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Queries.CompanyQueries;

namespace AuthService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<ApiResponse<List<CompanyDTO>>>> GetAll()
        {
            var query = new GetAllCompaniesQuery();
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetById")]
        public async Task<ActionResult<ApiResponse<CompanyDTO>>> GetById(Guid id)
        {
            var query = new GetCompanyByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetByDomain")]
        public async Task<ActionResult<ApiResponse<CompanyDTO>>> GetByDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return BadRequest(new ApiResponse<CompanyDTO>(false, "Domain is required"));

            var query = new GetCompanyByDomainQuery(domain);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetMyCompany")]
        public async Task<ActionResult<ApiResponse<CompanyDTO>>> GetMyCompany()
        {
            var companyIdRaw = User.FindFirst("companyId")?.Value;
            if (!Guid.TryParse(companyIdRaw, out var companyId))
                return Unauthorized(new ApiResponse<CompanyDTO>(false, "Invalid company session"));

            var query = new GetCompanyByIdQuery(companyId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("GetUsers")]
        public async Task<ActionResult<ApiResponse<List<UserGetDTO>>>> GetUsers(Guid companyId)
        {
            var query = new GetUsersByCompanyIdQuery(companyId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("CheckNameExists")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckNameExists(
            string companyName,
            Guid? excludeCompanyId = null
        )
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return BadRequest(new ApiResponse<bool>(false, "Company name is required"));

            var query = new CheckCompanyNameExistsQuery(companyName, excludeCompanyId);
            var result = await _mediator.Send(query);

            if (result.Success == false)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            var command = new DeleteCompanyCommand(id);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to delete company" });

            return Ok(result);
        }

        /// Actualiza el plan de servicio de una company
        [HttpPut("UpdateCompanyPlan")]
        public async Task<ActionResult<ApiResponse<CompanyPlanUpdateResultDTO>>> UpdateServicePlan(
            [FromBody] UpdateCompanyPlanDTO servicePlanDto
        )
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    new ApiResponse<CompanyPlanUpdateResultDTO>(
                        false,
                        $"Validation errors: {string.Join(", ", errors)}",
                        null!
                    )
                );
            }

            var command = new UpdateCompanyPlanCommand(servicePlanDto);
            var result = await _mediator.Send(command);

            if (result == null)
                return BadRequest(new { message = "Failed to update service plan" });

            // Retornar diferentes códigos según el resultado
            if (result.Success == true)
                return Ok(result);
            else if (result.Message?.Contains("not found") == true)
                return NotFound(result);
            else if (
                result.Message?.Contains("validation") == true
                || result.Message?.Contains("limit") == true
            )
                return BadRequest(result);
            else
                return StatusCode(500, result);
        }
    }
}
