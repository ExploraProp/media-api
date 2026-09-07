using Media.Domain.Events;
using Media.Domain.Services.Interfaces;
using MediatR;

namespace Media.Application.EventHandlers;

public sealed class MediaFileApprovedEventHandler(IMediaEventChannel mediaEventChannel)
    : INotificationHandler<FileApproved>
{
    public Task Handle(FileApproved notification, CancellationToken cancellationToken)
    {
        mediaEventChannel.Publish(
            notification.MediaFileId,
            new MediaFileEvent("fileApproved", notification));

        return Task.CompletedTask;
    }
}
