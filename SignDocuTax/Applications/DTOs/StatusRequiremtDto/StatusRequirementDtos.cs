namespace DTOs.StatusRequiremtDto;

public class ReadRequiremenStatustDtos
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateStatusRequirementDto
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateStatusRequirementDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}


public class DeleteStatusRequirementDto
{
    public required int Id { get; set; }

}

