


using Application.Common;
using AutoMapper;
using Infrastructure.Command.Form;
using Infrastructure.Context;
using MediatR;

public class UpdateFormInstanceHandlers: IRequestHandler<UpdateFormResponseCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateFormInstanceHandlers> _logger;

    public UpdateFormInstanceHandlers(TaxProStoreDbContext context, IMapper mapper, ILogger<UpdateFormInstanceHandlers> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateFormResponseCommand request, CancellationToken cancellationToken)
    {
        var response = await _context.FormResponses.FindAsync(request.Id);
        if (response == null)
        {
            return new ApiResponse<bool>(false, "Form response not found", false);
        }

        _mapper.Map(request.Response, response);
        _context.FormResponses.Update(response);
        await _context.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>(true, "Form response updated successfully", true);
    }
}
