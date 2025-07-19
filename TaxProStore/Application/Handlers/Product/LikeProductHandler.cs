using Application.Common;
using Domain.Entity.Products;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class LikeProductHandler : IRequestHandler<LikeProductCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;

    public LikeProductHandler(TaxProStoreDbContext context)
    {
        _context = context;
    }

   public async Task<ApiResponse<bool>> Handle(LikeProductCommand request, CancellationToken cancellationToken)
{
    var product = await _context.Products.FindAsync(request.Data.ProductId);
    if (product == null) return new ApiResponse<bool>(false,"Producto no encontrado");

    var feedback = await _context.ProductFeedbacks
        .FirstOrDefaultAsync(f => f.ProductId == request.Data.ProductId && f.UserId == request.Data.UserId);

    if (feedback != null)
    {
        if (feedback.IsLike == true)
            return new ApiResponse<bool>(true,"Ya diste like a este producto");

        feedback.IsLike = true;
    }
    else
    {
        _context.ProductFeedbacks.Add(new ProductFeedback
        {
            ProductId = request.Data.ProductId,
            UserId = request.Data.UserId,
            IsLike = true
        });
    }

    product.Likes += 1;
    await _context.SaveChangesAsync(cancellationToken);
    return new ApiResponse<bool>(true, "Like registrado correctamente");
}

}
