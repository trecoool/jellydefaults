using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using JellyDefaults.Configuration;
using JellyDefaults.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JellyDefaults.Api;

// Admin endpoints for the config page: available views, sort import, apply templates to existing users.
[ApiController]
[Route("JellyDefaults")]
[Authorize(Policy = "RequiresElevation")]
public class DefaultsController : ControllerBase
{
    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly DefaultsApplier _applier;
    private readonly SortDefaultsApplier _sorts;

    public DefaultsController(IUserManager userManager, IUserViewManager userViewManager, DefaultsApplier applier, SortDefaultsApplier sorts)
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _applier = applier;
        _sorts = sorts;
    }

    public record ViewDto(Guid Id, string Name);

    // Top-level views as an admin sees them (libraries + Playlists/Collections).
    [HttpGet("Views")]
    public ActionResult<IEnumerable<ViewDto>> GetViews() =>
        Ok(TopLevelViews().Select(v => new ViewDto(v.Id, v.Name)));

    private IEnumerable<BaseItem> TopLevelViews()
    {
        var admin = _userManager.GetUsers().First(u => u.HasPermission(PermissionKind.IsAdministrator));
        return _userViewManager.GetUserViews(new UserViewQuery { User = admin, IncludeHidden = true });
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

    public record SortTargetDto(string Name, int Tabs);

    // Main-tab sorts a user changed from the default, to import as template. Stale keys of deleted libraries are dropped.
    [HttpGet("Sorts")]
    public ActionResult<IEnumerable<SortDefault>> GetSorts([FromQuery] Guid userId)
    {
        var libraries = TopLevelViews().Select(v => v.Id).ToHashSet();
        return Ok(_sorts.ReadCustomSorts(userId).Where(s => libraries.Contains(s.LibraryId)));
    }

    // Returns each user that would change with its changed tab count; dryRun only lists them.
    [HttpPost("ApplySorts")]
    public ActionResult<IEnumerable<SortTargetDto>> ApplySorts([FromQuery] bool includeCustomized, [FromQuery] bool dryRun)
    {
        if (!SortDefaultsApplier.HasTemplate)
        {
            return BadRequest("Sort template is empty: import and save sorts first.");
        }

        var targets = _sorts.GetTargets(includeCustomized);
        if (!dryRun)
        {
            foreach (var target in targets)
            {
                _sorts.Apply(target.User, includeCustomized);
            }
        }

        return Ok(targets.Select(t => new SortTargetDto(t.User.Username, t.Tabs)));
    }
}
