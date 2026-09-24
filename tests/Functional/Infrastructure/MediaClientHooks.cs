using ExploraProp.Media.ApiClient;
using Functional.Infrastructure;
using Reqnroll;
using Reqnroll.BoDi;

namespace Media.Functional.Infrastructure;

[Binding]
public sealed class MediaClientHooks(IObjectContainer objectContainer)
{
    [BeforeScenario(Order = 1)]
    public void RegisterMediaApiClient(FeatureContext featureContext)
    {
        var worker = featureContext.Get<Worker>();
        var mediaApi = KiotaHttpClientFactory.CreateClient(
            worker.HttpClient,
            adapter => new MediaApiClient(adapter));
        objectContainer.RegisterInstanceAs(mediaApi);
    }
}
