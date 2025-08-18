namespace Application.Common.DTO;

public class DeleteEmailConfigDTO
{
    public required Guid CompanyId { get; set; }
    public required Guid DeletedByTaxUserId { get; set; }
}
