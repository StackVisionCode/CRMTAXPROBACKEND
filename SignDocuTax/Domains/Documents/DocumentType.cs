using System.Text.Json.Serialization;
using Common;

namespace Domain.Documents;

public class DocumentType : BaseEntity
{
 
    public required string Name { get; set; }
    public  string? Description { get; set; }
    [JsonIgnore]
    public  ICollection<Document>? Documents { get; set; }= new List<Document>();
}
