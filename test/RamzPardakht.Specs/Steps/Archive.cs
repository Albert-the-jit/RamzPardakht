using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Moq;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.WebApi.IntegrationTests;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.Specs.Steps;

[Binding]
public class Archive
{
    private readonly ScenarioContext _scenarioContext;
    private readonly CustomWebApplicationFactory _applicationFactory;

    public Archive(ScenarioContext scenarioContext, CustomWebApplicationFactory applicationFactory)
    {
        _applicationFactory = applicationFactory;
        _scenarioContext = scenarioContext;
    }

    [When(@"the ""(.*)"" send upload request containing simple image")]
    public async Task WhenTheSendUploadRequestContainingSimpleImage(string p0)
    {
        using var scope = _applicationFactory.Services.CreateScope();

        var scopedServices = scope.ServiceProvider;
        var service = scopedServices.GetRequiredService<Mock<IMinioClient>>();



        service.Should().NotBeNull();

        service.Setup(e => e.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => true);

        service.Setup(e => e.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new PutObjectResponse(HttpStatusCode.OK, "", new Dictionary<string, string>(), 100, Guid.NewGuid().ToString()));

        service.Setup(e => e.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(new Func<GetObjectArgs, CancellationToken, Task<ObjectStat>>(async (arg1, arg2) =>
            {
                PropertyInfo? propertyInfo = typeof(GetObjectArgs)
                    .GetProperty("CallBack", BindingFlags.Instance |
                                            BindingFlags.NonPublic |
                                            BindingFlags.Public);

                Action<Stream>? action = (Action<Stream>)propertyInfo?.GetValue(arg1)!;

                string path = @"ArchiveTestFiles/images.jpg";
                await using var stream = File.OpenRead(path);

                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, arg2);
                memoryStream.Seek(0, SeekOrigin.Begin);

                action?.Invoke(memoryStream);

                var statResponse = ObjectStat.FromResponseHeaders("xcvxcv", new Dictionary<string, string>());

                await Task.Delay(1000, arg2);
                return statResponse;
            }));



        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");


        string path = @"ArchiveTestFiles/images.jpg";
        await using var stream = File.OpenRead(path);

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        using var content = new MultipartFormDataContent
        {
            { new StreamContent(stream), "file", Path.GetFileName(path) }
        };

        var request = await client.PostAsync("/v1/Archive", content);
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [Then(@"the ""(.*)"" response body should contain the uploaded file unique identifier")]
    public async Task ThenTheResponseBodyShouldContainTheUploadedFileUniqueIdentifier(string p0)
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        var result = await res.Content.ReadFromJsonAsync<ArchiveModel>();

        result!.Id.Should().NotBeEmpty();

        _scenarioContext.Set(result, $"{p0}:{nameof(ArchiveModel)}");
    }


    [When(@"the ""(.*)"" send request to view uploaded file")]
    public async Task WhenTheSendRequestToViewUploadedFile(string p0)
    {
        var client = _scenarioContext.Get<HttpClient>($"{p0}:{nameof(HttpClient)}");
        var archiveModel = _scenarioContext.Get<ArchiveModel>($"{p0}:{nameof(ArchiveModel)}");

        var request = await client.GetAsync($"/v1/Archive/{archiveModel.Id}");
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }

    [When(@"the ""(.*)"" use uploaded file in ""(.*)"" usage")]
    public async Task WhenTheUseUploadedFileInUsage(string p0, string type)
    {
        var archiveModel = _scenarioContext.Get<ArchiveModel>($"{p0}:{nameof(ArchiveModel)}");

        var projectDbContext = _applicationFactory.Services.CreateScope().ServiceProvider.GetRequiredService<IProjectDbContext>();
        var archive = await projectDbContext.Archives.FirstOrDefaultAsync(x => x.Id == archiveModel.Id);

        archive!.Type = Enum.Parse<ArchiveType>(type);
        projectDbContext.Archives.Update(archive);
        await projectDbContext.SaveChangesAsync();

    }

    [Then(@"the ""(.*)"" response should contain uploaded file")]
    public async Task ThenTheResponseShouldContainUploadedFile(string p0)
    {
        var res = _scenarioContext.Get<HttpResponseMessage>($"{p0}:{nameof(HttpResponseMessage)}");
        byte[] bytes = await res.Content.ReadAsByteArrayAsync();
        string path = @"ArchiveTestFiles/images.jpg";
        await using var stream = File.OpenRead(path);
        var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        byte[] byteArray = memoryStream.ToArray();
        byteArray.SequenceEqual(bytes).Should().Be(true);


    }

    [When(@"the anonymous user send request to view ""(.*)"" uploaded file")]
    public async Task WhenTheAnonymousUserSendRequestToViewUploadedFile(string p0)
    {
        var client = _applicationFactory.CreateClient();
        var archiveModel = _scenarioContext.Get<ArchiveModel>($"{p0}:{nameof(ArchiveModel)}");

        var request = await client.GetAsync($"/v1/Archive/{archiveModel.Id}");
        _scenarioContext.Set(request, $"{p0}:{request.GetType().Name}");
    }
}
