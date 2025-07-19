using Application.Common;
using Application.Domain.Entity.Products;
using AutoMapper;
using Infrastructure.Command.Product;
using Infrastructure.Context;
using MediatR;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductHandler> _log;

    public CreateProductHandler(TaxProStoreDbContext db,
                                IMapper mapper,
                                ILogger<CreateProductHandler> log)
    {
        _log = log;
        _mapper = mapper;
           _db = db;
    }
   

    public async Task<ApiResponse<bool>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {

        // Here you would implement the logic to create a product.
        // This is a placeholder implementation.
        var ProductEntity = _mapper.Map<Product>(request.CreateProductDto);
        ProductEntity.Id = Guid.NewGuid();
        ProductEntity.CreatedAt = DateTime.UtcNow;
        ProductEntity.UpdatedAt = DateTime.UtcNow;
        _db.Products.Add(ProductEntity);
        await _db.SaveChangesAsync(cancellationToken);
        _log.LogInformation("Product created successfully with ID: {ProductId}", ProductEntity.Id);
        return new ApiResponse<bool>(
            true,
            "Product created successfully"
        );

       
    }
}