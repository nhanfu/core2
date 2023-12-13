using Core.Exceptions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Core.Controllers;

public class UserController(UserService _userSvc, TaskService _taskSvc) : ControllerBase
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

    [HttpGet("api/User/ReSendUser/{userId}")]
    public Task<string> ReSendUser(string userId)
    {
        return _userSvc.ResendUser(userId);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/svc")]
    public Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> ExecUserSvc([FromBody] SqlViewModel vm)
    {
        return _userSvc.RunUserSvc(vm);
    }

    [HttpPost("api/[Controller]/excel")]
    public Task<string> ExportExcel([FromBody] SqlViewModel vm)
    {
        return _userSvc.ExportExcel(vm);
    }

    [HttpPatch("api/v2/[Controller]", Order = 0)]
    public Task<bool> PatchAsync([FromBody] PatchVM patch)
    {
        patch.ByPassPerm = false;
        return _userSvc.SavePatch(patch);
    }

    [HttpDelete("api/[Controller]/HardDelete", Order = 0)]
    public Task<string[]> HardDeleteAsync([FromBody] SqlViewModel vm)
    {
        return _userSvc.HardDeleteAsync(vm);
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

    [HttpPost("api/[Controller]/Reader")]
    public Task<IEnumerable<IEnumerable<Dictionary<string, object>>>> Reader(
        [FromBody] SqlViewModel model)
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

    [HttpPost("api/feature/Clone")]
    public Task<bool> CloneFeatureAsync([FromBody] string id)
    {
        return _userSvc.CloneFeature(id);
    }

    [HttpDelete("api/feature/HardDelete")]
    public Task<bool> HardDeleteFeature([FromBody] List<string> ids)
    {
        return _userSvc.HardDeleteFeature(ids);
    }

    [HttpPost("/api/chat")]
    public Task<Chat> CreateAsync([FromBody] Chat entity)
    {
        return _taskSvc.Chat(entity);
    }

    [HttpPost("api/GetUserActive")]
    public Task<List<User>> GetUserActive()
    {
        return _taskSvc.GetUserActive();
    }
}