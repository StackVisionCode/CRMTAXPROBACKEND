using System.ComponentModel.DataAnnotations;

namespace DTOs.GeographyDTOs;

public class CountryDTO
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<StateDTO> States { get; set; } = new List<StateDTO>();
}

public class StateDTO
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
}
