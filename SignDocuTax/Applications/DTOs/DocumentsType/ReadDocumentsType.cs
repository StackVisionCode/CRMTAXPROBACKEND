
namespace DTOs.DocumentsType;
public class ReadDocumentsType
{
    public int Id { get; set; } // Para identificar el DocumentType a actualizar
    public required string Name { get; set; }
     public required string Description { get; set; }
}