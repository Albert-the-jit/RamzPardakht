using BoDi;
using RamzPardakht.WebApi.IntegrationTests;

namespace RamzPardakht.Specs.Hooks;
[Binding]
public sealed class Hooks
{
    private readonly IObjectContainer _objectContainer;

    public Hooks(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer;
    }

    [BeforeScenario]

    public void BeforeTestRun()
    {
        var applicationFactory = new CustomWebApplicationFactory();
        _objectContainer.RegisterInstanceAs(applicationFactory);
        _objectContainer.RegisterInstanceAs(applicationFactory.CreateClient());
    }
    [AfterScenario]
    public void AfterTestRun()
    {
        var applicationFactory = _objectContainer.Resolve<CustomWebApplicationFactory>();
        applicationFactory.Dispose();
    }
}
