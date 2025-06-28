namespace CustomerService.DTOs.ContactInfoDTOs;

public class EnableLoginDTO
{
    public Guid CustomerId { get; set; }
    public bool Enable { get; set; }
    public Guid? RoleId { get; set; }
    public string? Password { get; set; }
}
