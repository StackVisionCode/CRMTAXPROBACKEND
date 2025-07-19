using Application.Common;
using Application.Dtos;
using MediatR;

namespace Infrastructure.Command.Product;
public record  CreateProductCommand(CreateProductDto CreateProductDto) : IRequest<ApiResponse<bool>>;
