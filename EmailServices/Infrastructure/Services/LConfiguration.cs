using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class LConfiguration : IConfiguration
{
    private readonly EmailContext _context;
    private readonly ILogger<LConfiguration> _logs;
    private readonly IMapper _map;
    public LConfiguration(EmailContext context, ILogger<LConfiguration> logs, IMapper map)
    {
        _context = context;
        _logs = logs;
        _map = map;
    }
    public async Task<ConfigurationDTO> GetConfigurationByCompanyId(int CompanyId)
    {
            try
            {
                var result = await _context.EmailSettings.AsNoTracking().FirstOrDefaultAsync(a=>a.CompanyId==CompanyId);

                if (result is null)
                {
                    return null!;
                }

                var resultMap = _map.Map<ConfigurationDTO>(result);
                
                    return resultMap;
            }
            catch (Exception ex)
            {
                _logs.LogError(ex.Message,ex);
              return null!;
            }
    }
}