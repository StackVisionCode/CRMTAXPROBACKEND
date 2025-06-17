
using Application.Interfaces;
using Domain;
using Domain.Entities;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;


namespace Infraestructure.Repositories;

public class SignatureRepository : ISignatureRepository
{
    private readonly SignatureDbContext _context;

    public SignatureRepository(SignatureDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Signature signature,CancellationToken cancellationToken)
    {
        await _context.Signatures.AddAsync(signature, cancellationToken);
        await _context.SaveChangesAsync();
    }

    public async Task<Signature?> GetById(Guid id)
    {
        return await _context.Signatures
            .AsNoTracking().Where(s => s.Id == id)
            .FirstOrDefaultAsync();
        
    }
}
