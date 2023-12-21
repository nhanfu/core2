using Core.Exceptions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Controllers;

[Authorize]
public class UserController(UserService _userSvc) : ControllerBase
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

    [HttpPatch("api/v2/[Controller]", Order = 0)]
    public Task<int> PatchAsync([FromBody] PatchVM patch)
    {
        patch.ByPassPerm = false;
        return _userSvc.SavePatch(patch);
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
    [HttpGet("/{tenant?}/{area?}/{env?}/{feature?}")]
    public Task Index([FromRoute] string tenant = "system",
        [FromRoute] string area = "admin", [FromRoute] string env = "test")
    {
        return _userSvc.Launch(tenant, area, env);
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

    [AllowAnonymous]
    [HttpPost("SetStringToStorage")]
    public Task SetStringToStorage([FromBody] string key, [FromBody] string value) => _userSvc.SetStringToStorage(key, value);

    [HttpPost("NotifyDevice")]
    public ValueTask<bool> NotifyDevice([FromBody] MQEvent e) 
    {
        _userSvc.NotifyDevice(e);
        return new ValueTask<bool>(true);
    }
}