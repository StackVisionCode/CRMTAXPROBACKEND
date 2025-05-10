namespace DTOs.Signers
{
    public class CreateExternalSignerDto
    {
        public int DocumentId { get; set; }
        public int ContactId { get; set; } // ← Se envía solo el ContactId
        public int RequirementSignatureId { get; set; }
    }
    public class UpdateExternalSignerDto
    {
        public int Id { get; set; }
        public string? SignatureImageUrl { get; set; }
        public int? SignatureStatusId { get; set; }
    }

     public class ExternalSignerDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public required string SigningToken { get; set; }
        public int SignatureStatusId { get; set; }
        public DateTime InvitationSentDate { get; set; }
        public DateTime? SignedDate { get; set; }
        public string? SignatureImageUrl { get; set; }
        public string? IpAddress { get; set; }
    }
}
