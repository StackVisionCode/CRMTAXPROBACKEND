using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Application.Common.DTO;


public class EmailDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public required bool IsHtml { get; set; }
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Attachments { get; set; }
  
   

}