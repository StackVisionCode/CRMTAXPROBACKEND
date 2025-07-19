using Application.Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class RateProductHandler : IRequestHandler<RateProductCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;

    public RateProductHandler(TaxProStoreDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(RateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Data.ProductId, cancellationToken);
        if (product == null) return  new ApiResponse<bool>(false,"Producto no encontrado");

        // Calcular nueva calificación promedio
        product.Rating = ((product.Rating * product.TotalRatings) + request.Data.Rating) / (product.TotalRatings + 1);
        product.TotalRatings += 1;

        await _context.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>(true, "Rating actualizado correctamente");
    }
}
