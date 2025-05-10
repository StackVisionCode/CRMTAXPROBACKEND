namespace Dtos.SignatureTypeDto;

public class SignatureTypeDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class CreateSignatureTypeDto
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class UpdateSignatureTypeDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class DeleteSignatureTypeDto
{
    public required int Id { get; set; }

}

public class SignatureTypeResponseDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public  DateTime? CreatedAt { get; set; } 
    public  DateTime? UpdatedAt { get; set; } 
    public  DateTime? DeletedAt { get; set; } 
}

public class ReadOnlySignatureTypeDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GetByIdSignatureTypeDto
{
    public required int Id { get; set; }

}


