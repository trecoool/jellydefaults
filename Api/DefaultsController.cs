using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using JellyDefaults.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JellyDefaults.Api;

// Admin endpoints for the config page: available views, apply template to existing users.
[ApiController]
[Route("JellyDefaults")]
[Authorize(Policy = "RequiresElevation")]
public class DefaultsController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly DefaultsApplier _applier;

    public DefaultsController(IUserManager userManager, IUserViewManager userViewManager, DefaultsApplier applier)
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _applier = applier;
    }

    public record ViewDto(Guid Id, string Name);

    // Top-level views as an admin sees them (libraries + Playlists/Collections).
    [HttpGet("Views")]
    public ActionResult<IEnumerable<ViewDto>> GetViews()
    {
        var admin = _userManager.GetUsers().First(u => u.HasPermission(PermissionKind.IsAdministrator));
        var views = _userViewManager.GetUserViews(new UserViewQuery { User = admin, IncludeHidden = true });
        return Ok(views.Select(v => new ViewDto(v.Id, v.Name)));
    }

    // Returns the affected user names; dryRun only lists them.
    [HttpPost("Apply")]
    public async Task<ActionResult<IEnumerable<string>>> Apply([FromQuery] bool includeCustomized, [FromQuery] bool dryRun)
    {
        if (!DefaultsApplier.HasTemplate)
        {
            return BadRequest("Template is empty: save a library order first.");
        }

        var targets = _applier.GetTargets(includeCustomized);
        if (!dryRun)
        {
            foreach (var user in targets)
            {
                await _applier.ApplyAsync(user.Id).ConfigureAwait(false);
            }
        }

        return Ok(targets.Select(u => u.Username));
    }
}
