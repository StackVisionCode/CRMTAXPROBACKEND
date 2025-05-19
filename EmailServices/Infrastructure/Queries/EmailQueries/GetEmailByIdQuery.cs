using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailByIdQuery(int EmailId) : IRequest<EmailDTO?>;