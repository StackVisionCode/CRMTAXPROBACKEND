
using CompanyService.Application.Commons;
using MediatR;

namespace CompanyService.Infraestructure.Commands;
    public record class DeleteCompanyCommand(int Id) : IRequest<ApiResponse<bool>>;
    
       
