using Media.Domain.Services.Interfaces;

namespace Media.Application.Dto;

public sealed record MediaFileEventWaitDto(MediaFileEvent? Event, bool IsTimeout);
