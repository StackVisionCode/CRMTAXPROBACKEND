using Infraestructure.Commands;
using Application.DTOs;
using Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignaturesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISignatureRepository _signatureRepository;
    public SignaturesController(IMediator mediator, ISignatureRepository signatureRepository)
    {
        _mediator = mediator;
        _signatureRepository = signatureRepository;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSignature([FromBody] CreateSignatureDto dto)
    {
        var command = new CreateSignatureCommand(dto);
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

   
    [HttpGet("{id:guid}/download")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetById(Guid id)
{
    var signature = await _signatureRepository.GetById(id);

    if (signature == null)
        return NotFound("Signature not found.");

    // Si tienes el archivo en disco
    if (!string.IsNullOrWhiteSpace(signature.FilePath) && System.IO.File.Exists(signature.FilePath))
    {
        var fileBytes = await System.IO.File.ReadAllBytesAsync(signature.FilePath);
        return File(fileBytes, "image/png", $"{signature.Id}.png");
    }

    // Si tienes el base64 en la base de datos
    if (!string.IsNullOrWhiteSpace(signature.Base64Image))
    {
        try
        {
            var bytes = Convert.FromBase64String(signature.Base64Image);
            return File(bytes, "image/png", $"{signature.Id}.png");
        }
        catch
        {
            return StatusCode(500, "Invalid base64 image data.");
        }
    }

    return NotFound("No signature image available.");
}
}
