using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Commands;

public sealed record RejectMediaCommand(MediaFileId Id, ScanRejectionReason Reason) : IRequest<Result<bool>>;
