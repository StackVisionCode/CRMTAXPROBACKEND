using Domain.Entities;
using Application.DTOs;

namespace Application.Interfaces;
// Repository
public interface ISignatureRepository
{
    Task AddAsync(Signature Signar, CancellationToken cancellationToken = default);
    Task<Signature?> GetById(Guid id);
}