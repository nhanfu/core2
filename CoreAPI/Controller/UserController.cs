using Core.Exceptions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using Core.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Core.Extensions;

namespace Core.Controllers;

[Authorize]
public class UserController(UserService _userSvc, WebSocketService socketSvc) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("api/{tenant}/[Controller]/SignIn")]
    public async Task<ActionResult<Token>> SignInAsync([FromBody] LoginVM login, [FromRoute] string tenant)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        login.CompanyName ??= tenant;
        return await _userSvc.SignInAsync(login);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/SignOut")]
    public Task<bool> SignOutAsync([FromBody] Token token)
    {
        return _userSvc.SignOutAsync(token);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/Refresh")]
    public async Task<Token> RefreshAsync([FromBody] RefreshVM token)
    {
        if (token is null)
        {
            throw new ApiException("Token is required");
        }
        return await _userSvc.RefreshAsync(token);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/ForgotPassword")]
    public Task<bool> ForgotPassword([FromBody] LoginVM login)
    {
        return _userSvc.ForgotPassword(login);
    }

    [HttpGet("api/User/ReSendUser/")]
    public Task<string> ReSendUser(SqlViewModel vm)
    {
        return _userSvc.ResendUser(vm);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/svc")]
    public async Task<object> ExecUserSvc([FromBody] SqlViewModel vm)
    {
        var res = await _userSvc.RunUserSvc(vm);
        return res;
    }

    [HttpPost("api/[Controller]/excel")]
    public Task<string> ExportExcel([FromBody] SqlViewModel vm)
    {
        return _userSvc.ExportExcel(vm);
    }

    [HttpPost("api/[Controller]/del", Order = 0)]
    public Task<bool> HardDeleteAsync([FromBody] PatchVM patch)
    {
        patch.ByPassPerm = false;
        return _userSvc.HardDelete(patch);
    }

    [HttpPatch("api/v2/[Controller]", Order = 0)]
    public Task<int> PatchAsync([FromBody] PatchVM patch)
    {
        patch.ByPassPerm = false;
        return _userSvc.SavePatch(patch);
    }

    [HttpPatch("api/[Controller]/SavePatches", Order = 0)]
    public Task<int> SavePatches([FromBody] PatchVM[] patches)
    {
        patches.Action(x => x.ByPassPerm = false);
        return _userSvc.SavePatches(patches);
    }

    [HttpDelete("api/[Controller]/Deactivate", Order = 0)]
    public Task<string[]> DeactivateAsync([FromBody] SqlViewModel vm)
    {
        return _userSvc.DeactivateAsync(vm);
    }

    [HttpPost("api/[Controller]/ImportCsv")]
    public Task<bool> ImportCsv([FromForm] List<IFormFile> files, [FromQuery] string table, [FromQuery] string comId, [FromQuery] string connKey)
    {
        return _userSvc.ImportCsv(files, table, comId, connKey);
    }

    [HttpPost("api/[Controller]/File")]
    public Task<string> PostFileAsync([FromForm] IFormFile file, bool reup = false)
    {
        return _userSvc.PostFileAsync(file, reup);
    }

    [HttpPost("api/[Controller]/Image")]
    public Task<string> PostImageAsync([FromServices] IWebHostEnvironment host,
        [FromQuery] string name = "Captured", [FromQuery] bool reup = false)
    {
        return _userSvc.PostImageAsync(host, name, reup);
    }

    [HttpPost("api/[Controller]/EmailAttached")]
    public Task<bool> EmailAttached([FromBody] EmailVM email, [FromServices] IWebHostEnvironment host)
    {
        return _userSvc.EmailAttached(email, host);
    }

    [HttpPost("api/[Controller]/GeneratePdf")]
    public Task<IEnumerable<string>> GeneratePdf([FromBody] EmailVM email, [FromServices] IWebHostEnvironment host, bool absolute = false)
    {
        return _userSvc.GeneratePdf(email, host, absolute);
    }

    [HttpPost("api/[Controller]/DeleteFile")]
    public ValueTask<bool> DeleteFile([FromBody] string path)
    {
        return _userSvc.DeleteFile(path);
    }

    [HttpPost("api/[Controller]/ComQuery")]
    public Task<object> ComQuery([FromBody] SqlViewModel model)
    {
        return _userSvc.ComQuery(model);
    }

    [AllowAnonymous]
    [HttpGet("/{tenant?}/{area?}/{env?}/{path?}")]
    public Task Index([FromRoute] string tenant = "system", [FromRoute] string area = "admin", 
        [FromRoute] string env = "test", [FromRoute] string path = "")
    {
        return _userSvc.Launch(tenant, area, env, path);
    }

    [HttpPost("/api/chat")]
    public Task<Chat> CreateAsync([FromBody] Chat entity)
    {
        return _userSvc.Chat(entity);
    }

    [HttpPost("api/GetUserActive")]
    public IEnumerable<User> GetUserActive()
    {
        // Need to summarize info from all clusters
        return _userSvc.GetUserActive();
    }

    [HttpPost("NotifyDevice")]
    public Task<bool> NotifyDevice([FromBody] MQEvent e)
    {
        Task.Run(async () => await _userSvc.NotifyDevice(e));
        return Task.FromResult(true);
    }

    [HttpPost("api/cluster/add")]
    public Task AddCluster([FromBody] Node e)
    {
        return _userSvc.AddCluster(e);
    }

    [HttpPost("api/cluster/remove")]
    public Task RemoveCluster([FromBody] Node e)
    {
        return _userSvc.RemoveCluster(e);
    }

    [HttpPost("api/cluster/action")]
    public Task ClusterAction([FromBody] MQEvent e)
    {
        return socketSvc.MQAction(e);
    }
}