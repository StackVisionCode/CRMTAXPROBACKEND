using Common;
using CustomerService.DTOs.OccupationDTOs;
using MediatR;

namespace CustomerService.Coommands.OccupationCommands;

public record class CreateOccupationCommands(CreateOccupationDTO occupation)
    : IRequest<ApiResponse<bool>>;
