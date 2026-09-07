using Media.Domain.Entities.Definitions;
using Media.Domain.Enums.Properties;
using Media.Domain.ValueObjects.Ids;
using Media.Domain.ValueObjects.Properties;
using Media.Infrastructure.Context;
using Media.Infrastructure.Repositories;
using SharedKernel.Kernel.Results;

namespace Media.Unit.Infrastructure.Repositories;

[TestClass]
public class MediaPolicyRepoTests
{
    private MediaDbContext _context = null!;
    private MediaPolicyRepo _repo = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase($"MediaPolicyTestDb_{Guid.NewGuid()}")
            .Options;
        _context = new MediaDbContext(options);
        _repo = new MediaPolicyRepo(_context, new Logger<MediaPolicyRepo>(new LoggerFactory()));
        _ = _context.Database.EnsureCreated();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _context.DisposeAsync();
    }

    private static MediaPolicy CreatePolicy(OwnerType ownerType) =>
        MediaPolicy.Create(
            new MediaPolicyId(Guid.NewGuid()),
            ownerType,
            MediaMaxFileSize.FromDatabase(8 * 1024 * 1024),
            AllowedContentTypes.Jpeg | AllowedContentTypes.Png,
            MediaMaxCount.FromDatabase(50),
            AccessLevel.Public,
            watermarkEnabled: false);

    [TestMethod]
    public async Task GetByOwnerTypeAsync_WhenPolicyExists_ShouldReturnSuccess()
    {
        var policy = CreatePolicy(OwnerType.Listing);
        _context.MediaPolicy.Add(policy);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByOwnerTypeAsync(OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(OwnerType.Listing, result.Value!.OwnerType);
    }

    [TestMethod]
    public async Task GetByOwnerTypeAsync_WhenPolicyMissing_ShouldReturnNotFound()
    {
        var result = await _repo.GetByOwnerTypeAsync(OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(SharedFailures.NotFound(nameof(MediaPolicy)).Code.Value, result.Error!.Code.Value);
    }

    [TestMethod]
    public async Task GetByOwnerTypeAsync_WhenDatabaseUnavailable_ShouldReturnInternalServerError()
    {
        await _context.DisposeAsync();

        var result = await _repo.GetByOwnerTypeAsync(OwnerType.Listing, CancellationToken.None);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual(HttpStatusCode.InternalServerError, result.Error!.StatusCode);
    }
}
