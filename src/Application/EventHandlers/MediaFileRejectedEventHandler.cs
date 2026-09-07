using Media.Domain.Events;
using Media.Domain.Services.Interfaces;
using MediatR;

namespace Media.Application.EventHandlers;

public sealed class MediaFileRejectedEventHandler(IMediaEventChannel mediaEventChannel)
    : INotificationHandler<FileRejected>
{
    public Task Handle(FileRejected notification, CancellationToken cancellationToken)
    {
        mediaEventChannel.Publish(
            notification.MediaFileId,
            new MediaFileEvent("fileRejected", notification));

        return Task.CompletedTask;
    }
}
