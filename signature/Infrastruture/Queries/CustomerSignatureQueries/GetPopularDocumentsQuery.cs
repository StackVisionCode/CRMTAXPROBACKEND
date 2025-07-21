using Application.DTOs.CustomerSignatureDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastructure.Queries.CustomerSignatureQueries;

/// <summary>
/// Query para obtener documentos populares de un cliente
/// </summary>
public record GetPopularDocumentsQuery(Guid CustomerId)
    : IRequest<ApiResponse<List<PopularDocumentDto>>>;
