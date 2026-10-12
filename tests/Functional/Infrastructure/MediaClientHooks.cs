using ExploraProp.Media.ApiClient;
using ExploraProp.Session.Internal.ApiClient;
using Functional.Application;
using Functional.Domain.Enums.Properties;
using Functional.Domain.ValueObjects.Properties;
using Reqnroll;
using Reqnroll.BoDi;

namespace Media.Functional.Infrastructure;

[Binding]
public sealed class MediaClientHooks(IObjectContainer objectContainer)
{
    [BeforeScenario(Order = 1)]
    public void RegisterMediaApiClients(FeatureContext featureContext)
    {
        var worker = featureContext.Get<Worker>();
        var mediaBase = HostName.For(ApplicationProgrammingInterface.Media).BaseUrl.TrimEnd('/');
        var sessionBase = HostName.For(ApplicationProgrammingInterface.Session).BaseUrl.TrimEnd('/');

        objectContainer.RegisterInstanceAs(new MediaApiClient(mediaBase, worker.HttpClient));
        objectContainer.RegisterInstanceAs(new SessionInternalApiClient(sessionBase, worker.HttpClient));
    }
}
