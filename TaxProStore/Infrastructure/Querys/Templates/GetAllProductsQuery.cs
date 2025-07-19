
using Application.Common;
using Application.Domain.Entity.Products;
using MediatR;

namespace Infrastructure.Querys.Templates;
public record GetAllProductsQuery : IRequest<ApiResponse<List<Product>>>;