namespace Dtos.FirmsStatusDto;

public class FirmStatusDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateFirmStatusDto
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateFirmStatusDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}


public class DeleteFirmStatusDto
{
    public required int Id { get; set; }

}

public class FirmStatusDtoResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}
public class ResponseInfo
{
    public int Id { get; set; }
  
}

