
using Application.Common;
using Application.Domain.Entity.Products;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Querys.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Marketplace;

public class GetAllProductsHandler : IRequestHandler<GetAllProductsQuery, ApiResponse<List<Product>>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;

    public GetAllProductsHandler(TaxProStoreDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<Product>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {

        var products = await _context.Products.Include(p => p.Templates).ToListAsync();
        var result = _mapper.Map<List<Product>>(products);
        return new ApiResponse<List<Product>>(true, "Productos listados", result);

    }
}
