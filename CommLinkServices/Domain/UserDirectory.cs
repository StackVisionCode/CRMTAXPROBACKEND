namespace CommLinkServices.Domain;

public class UserDirectory
{
    public Guid UserId { get; set; }
    public string UserType { get; set; } = ""; // TaxUser | Customer
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsOnline { get; set; }
}
