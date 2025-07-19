using Application.Common;
using Application.Dtos.Product;
using MediatR;

public record LikeProductCommand(LikeProductDto Data) : IRequest<ApiResponse<bool>>;
