using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Community.FrontendBlockPreview.Services;

namespace Umbraco.Community.FrontendBlockPreview.Controllers;

public class CreateBlockPreviewTokenRequestModel
{
    public Guid DocumentKey { get; set; }

    public string PropertyAlias { get; set; } = string.Empty;

    public Guid ContentKey { get; set; }

    public string? Culture { get; set; }

    /// <summary>
    /// The Block List / Block Grid value from the backoffice property context, including unsaved changes.
    /// </summary>
    public JsonElement Value { get; set; }
}

public record BlockPreviewTokenResponseModel(string Token, string Url);

public record BlockPreviewErrorResponseModel(string Error);

[VersionedApiBackOfficeRoute(Constants.ManagementApiRoute)]
[ApiExplorerSettings(GroupName = "Frontend Block Preview")]
public class FrontendBlockPreviewManagementController : ManagementApiControllerBase
{
    private readonly IBlockPreviewService _blockPreviewService;

    public FrontendBlockPreviewManagementController(IBlockPreviewService blockPreviewService)
        => _blockPreviewService = blockPreviewService;

    /// <summary>
    /// Stores the unsaved block value and returns the URL that renders it.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(BlockPreviewTokenResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlockPreviewErrorResponseModel), StatusCodes.Status400BadRequest)]
    public IActionResult CreateToken([FromBody] CreateBlockPreviewTokenRequestModel model)
    {
        var result = _blockPreviewService.CreateToken(
            model.DocumentKey,
            model.PropertyAlias,
            model.ContentKey,
            model.Culture,
            model.Value.GetRawText());

        return result.Token is null || result.Url is null
            ? BadRequest(new BlockPreviewErrorResponseModel(result.Error ?? "The block could not be previewed."))
            : Ok(new BlockPreviewTokenResponseModel(result.Token, result.Url));
    }
}
