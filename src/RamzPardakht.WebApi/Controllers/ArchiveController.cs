using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Minio;
using Minio.DataModel.Args;
using RamzPardakht.ApplicationCore.Contracts;
using RamzPardakht.ApplicationCore.Entities;
using RamzPardakht.ApplicationCore.Resources;
using RamzPardakht.WebApi.Common;
using RamzPardakht.WebApi.Models;

namespace RamzPardakht.WebApi.Controllers;

[ApiController]
[Route("v{version:apiVersion}/[controller]")]
[ApiVersion(1)]
[ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
[ProducesResponseType((int)HttpStatusCode.OK)]
[Authorize]
public class ArchiveController : ControllerBase
{
    private readonly IProjectDbContext _projectDbContext;
    private readonly Mapper _mapper;
    private readonly ILogger<ArchiveController> _logger;
    private readonly IStringLocalizer<SharedResource> _stringLocalizer;
    private readonly IMinioClient _minioClient;

    private string[] permittedExtensions = { ".png", ".jpg", ".jpeg", ".pdf", ".svg" };

    public ArchiveController(
        IProjectDbContext projectDbContext,
        Mapper mapper,
        IStringLocalizer<SharedResource> stringLocalizer,
        IMinioClient minioClient,
        ILogger<ArchiveController> logger
        )
    {
        _projectDbContext = projectDbContext;
        _mapper = mapper;
        _stringLocalizer = stringLocalizer;
        _minioClient = minioClient;
        _logger = logger;
    }

    [HttpPost]
    [Produces("application/json")]
    public async Task<ActionResult<ArchiveModel>> Upload(IFormFile file)
    {
        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
            return ValidationProblem(_stringLocalizer["InvalidFileExtension"]);

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);


        bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket("archive"));
        if (!found)
        {
            await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket("archive"));
        }

        var id = Guid.NewGuid();

        var readStream = file.OpenReadStream();

        var putObjectResponse = await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket("archive")
                .WithObject(id.ToString())
                .WithContentType(ext)
                .WithObjectSize(readStream.Length)
                .WithStreamData(readStream));


        if (!string.IsNullOrEmpty(putObjectResponse.ObjectName))
        {
            var archive = new Archive()
            {
                Id = id,
                Type = ArchiveType.Public,
                FileExtension = ext,
            };

            await _projectDbContext.Archives.AddAsync(archive);
            await _projectDbContext.SaveChangesAsync();

            return _mapper.ToModel(archive);
        }

        _logger.LogError("archive service did not return id");

        return new StatusCodeResult(503);

    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        bool isAuthenticated = User?.Identity?.IsAuthenticated ?? false;

        var archive = await _projectDbContext.Archives.FirstOrDefaultAsync(x => x.Id == id && x.Type != ArchiveType.UnUsed, cancellationToken);

        if (archive is null)
            return NotFound();

        if (archive.Type == ArchiveType.Internal && !isAuthenticated)
            return NotFound();
        var downloadStream = new MemoryStream();

        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket("archive")
                .WithObject(id.ToString())
                .WithCallbackStream((stream) =>
                {
                    stream.CopyTo(downloadStream);
                    downloadStream.Seek(0, SeekOrigin.Begin);
                }), cancellationToken);

        new FileExtensionContentTypeProvider().TryGetContentType(Path.GetRandomFileName() + archive.FileExtension, out string? contentType);

        return File(downloadStream, contentType!);

    }

}

