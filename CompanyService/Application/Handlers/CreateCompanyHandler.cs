using AutoMapper;
using CompanyService.Application.Commons;
using CompanyService.Domains;
using CompanyService.Infraestructure.Commands;
using Infraestructure.Context;
using MediatR;

namespace CompanyService.Application.Handlers;



public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, ApiResponse<bool>>
{


    private readonly CompanyDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCompanyHandler> _logger;
 
    public CreateCompanyHandler(IMapper mapper, CompanyDbContext context, ILogger<CreateCompanyHandler> logger )
    {
        _mapper = mapper;
        _context = context;
        _logger = logger;
      
    }

    public async Task<ApiResponse<bool>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Companydto == null)
            {
                return  new ApiResponse<bool>(false, "Company data is null",false);
            }
          
            var company = _mapper.Map<Company>(request.Companydto);
            company.CreatedAt = DateTime.UtcNow;
            company.IActive= true;
            await _context.Companies.AddAsync(company, cancellationToken);
            bool result = await  _context.SaveChangesAsync(cancellationToken) >0?true:false;
            if (!result)
            {
                _logger?.LogError("Failed to create company: {Company}", request.Companydto);
                // Log the error or handle it as needed
                return new ApiResponse<bool>(false, "Failed to create company", false);
            }
            _logger?.LogInformation("Company created successfully: {Company}", request.Companydto);
            return new ApiResponse<bool>(true, "Company created successfully", true);

            
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "An error occurred while creating the company: {Company}", request.Companydto);
            return new ApiResponse<bool>(false, "An error occurred while creating the company", false);
           
        }
    }

    
}