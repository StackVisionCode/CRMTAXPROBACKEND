using Application.Common;
using Domain.Entity.Products;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Handlers.Product;

public class DislikeProductHandler : IRequestHandler<DislikeProductCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;

    public DislikeProductHandler(TaxProStoreDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(DislikeProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Data.ProductId, cancellationToken);
        if (product == null) return  new ApiResponse<bool>(false,"Producto no encontrado");
        var feedback = await _context.ProductFeedbacks
        .FirstOrDefaultAsync(f => f.ProductId == request.Data.ProductId && f.UserId == request.Data.UserId);

        if (feedback != null)
        {
            if (feedback.IsLike == false)
                return new ApiResponse<bool>(true,"Ya diste dislike a este producto");

            feedback.IsLike = false;
        }
        else
        {
            _context.ProductFeedbacks.Add(new ProductFeedback
            {
                ProductId = request.Data.ProductId,
                UserId = request.Data.UserId,
                IsLike = false
            });
        }

        product.Dislikes += 1;
        await _context.SaveChangesAsync(cancellationToken);
        return  new ApiResponse<bool>(true, "Dislike agregado correctamente");
    }
}
