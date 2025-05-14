namespace DTOs.Documents
{
    // === Creación ===
    public class CreateDocumentRequest
    {
      
        public required IFormFile File { get; set; }
        
        
        public required string Name { get; set; }
        
       
        public required int DocumentTypeId { get; set; }

         public required int DocumentStatusId { get; set; }
        
       
        public required int CompanyId { get; set; }
        
       
        public required int TaxUserId { get; set; }
    }

    public class CreateDocumentWithSignersRequest : CreateDocumentRequest
    {
        public List<int> RegisteredSignerIds { get; set; } = new List<int>();
        public List<ExternalSignerDto> ExternalSigners { get; set; } = new List<ExternalSignerDto>();
        public SignatureSettingsDto SignatureSettings { get; set; }
    }

    public class ExternalSignerDto
    {
       
        public required string Email { get; set; }
        
     
        public required string Name { get; set; }
        
        public string? Phone { get; set; }
    }

    public class SignatureSettingsDto
    {
       
        public required DateTime ExpiryDate { get; set; }
        
      
        public required string ConsentText { get; set; }
        
        public bool RequireConsent { get; set; } = true;
    }

    // === Actualización ===
    public class UpdateDocumentRequest
    {
       
        public required int DocumentId { get; set; }
        
        public string? Name { get; set; }
        public int? DocumentStatusId { get; set; }
        public int? DocumentTypeId { get; set; }
    }

    // === Firmas ===
    public class SignDocumentRequest
    {
        
        public required int DocumentId { get; set; }
        
        public int? TaxUserId { get; set; } // Para registrados
        
        
        public string? Email { get; set; } // Para externos
        
        public string? Token { get; set; } // Para externos
        
       
        public required string SignatureData { get; set; } // Base64 o JSON
        
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }

    // === Responses ===
    public class DocumentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public bool IsSigned { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DocumentDetailResponse : DocumentResponse
    {
        public List<SignerInfoResponse> Signers { get; set; } = new List<SignerInfoResponse>();
        public List<SignatureEventResponse> SignatureHistory { get; set; } = new List<SignatureEventResponse>();
    }

    public class SignerInfoResponse
    {
        public int? UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Status { get; set; } // "Pending", "Signed", "Rejected"
        public DateTime? SignedDate { get; set; }
    }

    public class SignatureEventResponse
    {
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } // "View", "Sign", "Reject"
        public string IpAddress { get; set; }
        public string DeviceInfo { get; set; }
    }
}