using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyService.Application.Commons;
using CompanyService.Application.DTOs;
using CompanyService.Infraestructure.Commands;
using CompanyService.Infraestructure.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanyService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        

         
            private readonly ILogger<CompanyController> _logger;
            private readonly IMediator _mediator;
        public CompanyController(ILogger<CompanyController> logger, IMediator mediator)
        {

            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("create")]
            public async Task<ActionResult<ApiResponse<bool>>> CreateCompany([FromBody] CompanyDto company)
            {
                if (company == null)
                {
                    return BadRequest("Invalid company data.");
                }
    
                 var command = new CreateCompanyCommand(company); 
                   


               return Ok(await _mediator.Send(command));
             
            }
        [HttpGet("getall")]
        public async Task<ActionResult<ApiResponse<List<CompanyDto>>>> GetAllCompanies()
        {
            try
            {
                var response = await _mediator.Send(new GetAllCompanyQueries());
                if (response == null || response.Data == null || response.Data.Count == 0)
                {
                    return NotFound("No companies found.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving companies");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("getbyid")]
        public async Task<ActionResult<ApiResponse<CompanyDto>>> GetCompanyById(int id)
        {
            try
            {
                var response = await _mediator.Send(new GetCompanyByIdQueries(id));
                if (response == null || response.Data == null)
                {
                    return NotFound("Company not found.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving the company with ID: {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateCompany([FromBody] CompanyDto company)
        {
            if (company == null)
            {
                return BadRequest("Invalid company data.");
            }

            try
            {
                var response = await _mediator.Send(company);
                if (response == null || !(response as ApiResponse<bool>)?.Success == true)
                {
                    return NotFound("Company not found.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the company");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpDelete("delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCompany(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid company ID.");
            }

            try
            {
                var response = await _mediator.Send(new DeleteCompanyCommand(id));
                if (response == null || !(response as ApiResponse<bool>)?.Success == true)
                {
                    return NotFound("Company not found.");
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting the company with ID: {CompanyId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }}