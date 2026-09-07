using ExploraProp.Media.ApiClient;
using Functional.Infrastructure;
using Reqnroll;
using Reqnroll.BoDi;

namespace Media.Functional.Infrastructure;

[Binding]
public sealed class MediaClientHooks(IObjectContainer objectContainer)
{
    private const string WorkerKey = "FunctionalWorker";

    [BeforeScenario(Order = 1)]
    public void RegisterMediaApiClient(FeatureContext featureContext)
    {
        var worker = featureContext.Get<Worker>(WorkerKey);
        var mediaApi = KiotaHttpClientFactory.CreateClient(
            worker.HttpClient,
            adapter => new MediaApiClient(adapter));
        objectContainer.RegisterInstanceAs(mediaApi);
    }
}
