namespace DTOs.DocumentsType;

    public class UpdateDocumentsTypeDTo
    {
        public int Id { get; set; } // Para identificar el DocumentType a actualizar
        public string Name { get; set; } = null!; // requerido
        public string? Description { get; set; } // opcional
    }
