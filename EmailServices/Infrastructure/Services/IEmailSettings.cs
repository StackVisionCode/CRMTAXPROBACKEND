

using Application.Common.DTO;

namespace Infrastructure.Services;

public interface IEmailSettings{

    Task<EmailSettingsDTO> GetConfigurationByCompanyId(int CompanyId);

}