using Application.Common;
using AutoMapper;
using Infrastructure.Command.Form;
using Infrastructure.Context;
using MediatR;

namespace Handlers.Form;

public class CreateFormInstanceHandlers : IRequestHandler<CreateFormIntanceCommads, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly ILogger<CreateFormInstanceHandlers> _log;
    private readonly IMapper _mapper;

    public CreateFormInstanceHandlers(
        TaxProStoreDbContext db,
        ILogger<CreateFormInstanceHandlers> log,
        IMapper mapper)
    {
        _db = db;
        _log = log;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(CreateFormIntanceCommads request, CancellationToken cancellationToken)
    {
        _log.LogInformation("Handling CreateFormInstanceCommands for FormIntaceDto: {FormIntaceDto}", request.FormInstanceDto);
        
        if (request.FormInstanceDto == null)
        {
            _log.LogError("FormIntaceDto is null.");
            return new ApiResponse<bool>(false, "Form instance data is required.");
        }

        var formInstanceEntity = _mapper.Map<FormInstance>(request.FormInstanceDto);
        formInstanceEntity.Id = Guid.NewGuid();
        formInstanceEntity.CreatedAt = DateTime.UtcNow;
        formInstanceEntity.UpdatedAt = DateTime.UtcNow;

        await _db.FormInstances.AddAsync(formInstanceEntity);
        await _db.SaveChangesAsync(cancellationToken);

        if (formInstanceEntity.Id == Guid.Empty)
        {
            _log.LogError("Failed to create form instance.");
            return new ApiResponse<bool>(false, "Failed to create form instance.");
        }

        _log.LogInformation("Form instance created successfully with ID: {FormInstanceId}", formInstanceEntity.Id);
        return new ApiResponse<bool>(true, "Form instance created successfully.");
    }
}