using AutoMapper;
using Commands.SignatureTypes;
using Common;
using Infraestructure.Context;
using MediatR;
using Domains.Signatures;


namespace Handlers.SignatureTypeHandlers;

    public class CreateSignatureTypeHandler : IRequestHandler<CreateSignatureTypeCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public CreateSignatureTypeHandler(ApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<ApiResponse<bool>> Handle(CreateSignatureTypeCommand request, CancellationToken cancellationToken)
        {
           var entity = _mapper.Map<Domains.Signatures.SignatureType>(request.SignatureType);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SignatureTypes.AddAsync(entity, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            return new ApiResponse<bool>(result, result ? "SignatureType created successfully" : "Failed to create SignatureType", result);
        }
    }

