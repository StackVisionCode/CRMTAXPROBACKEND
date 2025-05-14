// Commands
using Common;
using DTOs.Documents;
using MediatR;

public record CreateDocumentCommand(CreateDocumentRequest Request) : IRequest<ApiResponse<DocumentResponse>>;
public record CreateDocumentWithSignersCommand(CreateDocumentWithSignersRequest Request) : IRequest<ApiResponse<DocumentResponse>>;
public record UpdateDocumentCommand(UpdateDocumentRequest Request) : IRequest<ApiResponse<bool>>;
public record DeleteDocumentCommand(int DocumentId) : IRequest<ApiResponse<bool>>;
public record SignDocumentCommand(SignDocumentRequest Request) : IRequest<ApiResponse<bool>>;
public record SendReminderCommand(SignDocumentRequest Request) : IRequest<ApiResponse<bool>>;

// Queries
public record GetDocumentByIdQuery(int DocumentId) : IRequest<ApiResponse<DocumentDetailResponse>>;
<<<<<<< HEAD
public record GetDocumentSignersQuery(int DocumentId) : IRequest<ApiResponse<List<SignerInfoResponse>>>;
=======
public record class GetDocumentSigners(int DocumentId) : IRequest<ApiResponse<List<ExternalSignerDto>>>;
>>>>>>> 4b49bd843ef322600271ae0810b969304e69192e
public record GetPendingDocumentsQuery(int UserId) : IRequest<ApiResponse<List<DocumentResponse>>>;
public record GetDocumentByTokenQuery(string Token) : IRequest<ApiResponse<DocumentDetailResponse>>;