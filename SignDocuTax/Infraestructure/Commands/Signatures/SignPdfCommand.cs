using MediatR;

namespace Infraestructure.Commands.Signatures;

public class SignPdfCommand : IRequest<string>
{
    public string InputFileName { get; set; }  // ej: uploaded.pdf
    public string OutputFileName { get; set; } // ej: signed_document.pdf
}
