using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Infrastructure.Context;
using Media.Infrastructure.Repositories;
using Media.Unit.TestSupport;
using SharedKernel.Kernel.Results;

namespace Media.Unit.Infrastructure.Repositories;

[TestClass]
public class MediaRepoTests
{
    private MediaDbContext _context = null!;
    private MediaRepo _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"MediaTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new MediaDbContext(options);
        _repo = new MediaRepo(_context, new Logger<MediaRepo>(new LoggerFactory()));
        _ = _context.Database.EnsureCreated();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _context.DisposeAsync();
    }

    private static MediaFile CreateMediaFile(Guid ownerId, OwnerType ownerType) =>
        MediaTestFactory.CreateReadyMediaFile(ownerId: new OwnerId(ownerId));

    [TestMethod]
    public async Task CountByOwnerAsync_WhenFilesExist_ShouldReturnCount()
    {
        var ownerId = Guid.NewGuid();
        _context.MediaFile.AddRange(
            CreateMediaFile(ownerId, OwnerType.Listing),
            CreateMediaFile(ownerId, OwnerType.Listing),
            CreateMediaFile(Guid.NewGuid(), OwnerType.Listing));
        await _context.SaveChangesAsync();

        var result = await _repo.CountByOwnerAsync(ownerId, OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.Value);
    }

    [TestMethod]
    public async Task CountByOwnerAsync_WhenNoFilesExist_ShouldReturnZero()
    {
        var result = await _repo.CountByOwnerAsync(Guid.NewGuid(), OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(0, result.Value);
    }

    [TestMethod]
    public async Task CountByOwnerAsync_WhenDatabaseUnavailable_ShouldReturnInternalServerError()
    {
        await _context.DisposeAsync();

        var result = await _repo.CountByOwnerAsync(Guid.NewGuid(), OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.Error!.StatusCode);
    }
}
