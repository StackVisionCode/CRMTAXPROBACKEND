using Signaturex;

namespace Services;

public interface ISignatureRepository
{
    Task AddAsync(Signature signature);
}