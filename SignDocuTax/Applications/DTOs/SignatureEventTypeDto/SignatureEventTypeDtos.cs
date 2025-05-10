namespace DTOs.SignatureEventTypeDto
{
    public class ReadSignatureEventTypeDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class CreateSignatureEventTypeDto
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateSignatureEventTypeDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class DeleteSignatureEventTypeDto
    {
        public required int Id { get; set; }
    }
}
