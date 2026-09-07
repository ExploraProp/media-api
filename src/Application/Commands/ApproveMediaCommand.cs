using Media.Application.Dto;
using Media.Domain.ValueObjects.Ids;
using MediatR;
using SharedKernel.Kernel.Results;

namespace Media.Application.Commands;

public sealed record ApproveMediaCommand(MediaFileId Id) : IRequest<Result<MediaFileDto>>;
