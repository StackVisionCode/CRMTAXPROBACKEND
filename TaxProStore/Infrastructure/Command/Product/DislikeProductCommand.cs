using Application.Common;
using Application.Dtos.Product;
using MediatR;

public record DislikeProductCommand(DislikeProductDto Data) : IRequest<ApiResponse<bool>>;
