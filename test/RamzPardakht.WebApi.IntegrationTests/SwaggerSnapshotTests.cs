namespace RamzPardakht.WebApi.IntegrationTests;

[UsesVerify]
public class SwaggerSnapshotTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _customWebApplicationFactory;

    public SwaggerSnapshotTests(CustomWebApplicationFactory customWebApplicationFactory)
    {
        _customWebApplicationFactory = customWebApplicationFactory;
    }

    [Fact]
    public async Task Verify_v1_swagger_json_snapshot()
    {
        var v1SwaggerJson = await _customWebApplicationFactory.CreateClient()
            .GetAsync("api-docs/v1/swagger.json");

        await Verify(v1SwaggerJson.Content.ReadAsStringAsync());
    }
}
