using Core.Exceptions;
using Core.Extensions;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Services;
using CoreAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Core.Controllers;

[Authorize]
public class UserController(
    UserService _userSvc,
    AuthService _authSvc,
    PdfService _pdfService,
    ExcelService _excelService,
    OpenAIHttpClientService _openAIHttpClientService,
    AuthService _auth,
    IPatchService _patchService,
    IQueryService _queryService,
    IFileService _fileService,
    IMetadataService _metadataService,
    IHttpContextAccessor _httpContextAccessor) : ControllerBase
{
    private void SetUserContextToServices()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return;

        var userId = user.FindFirst("UserId")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantCode = user.FindFirst("TenantCode")?.Value?.ToUpper();
        var env = user.FindFirst("Environment")?.Value;
        var roleIds = user.FindAll("RoleId").Select(c => c.Value).ToList();
        var roleNames = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (_patchService is PatchService ps) ps.SetUserContext(userId, tenantCode, env, roleIds);
        if (_queryService is QueryService qs) qs.SetUserContext(userId, tenantCode, env, roleIds, roleNames, null, null, null);
        if (_fileService is FileService fs) fs.SetUserContext(userId, tenantCode);
        if (_metadataService is MetadataService ms) ms.SetUserContext(tenantCode, roleIds);
    }
    [AllowAnonymous]
    [HttpPost("/api/auth/login")]
    public async Task<ActionResult<Token>> SignInAsync([FromBody] LoginVM login)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return await _auth.SignInAsync(login);
    }

    [AllowAnonymous]
    [HttpGet("/api/health")]
    public bool Health()
    {
        return true;
    }

    [HttpPost("/api/CreateUser")]
    public async Task<Partner> CreateUser([FromBody] Partner entity)
    {
        return await _authSvc.CreateUser(entity);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/SignOut")]
    public Task<bool> SignOutAsync([FromBody] Token token)
    {
        return _auth.SignOutAsync(token);
    }

    [HttpPost("api/CreateHtml")]
    public async Task<string> CreateHtml([FromBody] CreateHtmlVM token, [FromServices] IServiceProvider iServiceProvider, [FromServices] IConfiguration configuration)
    {
        return await _pdfService.CreateHtml(token, BgExt.GetConnectionString(iServiceProvider, configuration, "logistics"));
    }

    [HttpPost("api/CreateExcel")]
    public async Task<string> CreateExcel([FromBody] CreateHtmlVM token, [FromServices] IServiceProvider iServiceProvider, [FromServices] IConfiguration configuration)
    {
        return await _excelService.CreateExcelFile(token, BgExt.GetConnectionString(iServiceProvider, configuration, "logistics"));
    }

    [HttpPost("api/OpenAI")]
    public async Task<string> OpenAI([FromBody] string prompt)
    {
        var response = await _openAIHttpClientService.GetChatGPTResponse(prompt);
        return response;
    }

    [AllowAnonymous]
    [HttpPost("/api/auth/refreshToken")]
    public async Task<Token> RefreshAsync([FromBody] RefreshVM token)
    {
        if (token is null)
        {
            throw new ApiException("Token is required");
        }
        return await _auth.RefreshAsync(token);
    }

    [HttpPost("api/[Controller]/UpdatePassword")]
    public Task<bool> UpdatePassword([FromBody] UpdatePasswordVM login)
    {
        return _auth.UpdatePassword(login);
    }

    [HttpPost("/api/GenPdf")]
    public async Task<string> HtmlToPdf([FromBody] PdfVM vm)
    {
        return await _pdfService.HtmlToPdf(vm);
    }

    [HttpGet("api/User/ReSendUser/")]
    public Task<string> ReSendUser(SqlViewModel vm)
    {
        return _authSvc.ResendUser(vm);
    }

    [HttpPatch("api/[Controller]/SavePatches", Order = 0)]
    public Task<int> SavePatches([FromBody] PatchVM[] patches)
    {
        SetUserContextToServices();
        patches.Action(x => x.ByPassPerm = false);
        return _patchService.SavePatches(patches);
    }

    [HttpDelete("api/[Controller]/Deactivate", Order = 0)]
    public Task<string[]> DeactivateAsync([FromBody] SqlViewModel vm)
    {
        SetUserContextToServices();
        return _patchService.DeactivateAsync(vm);
    }

    [HttpPost("api/[Controller]/ImportCsv")]
    public Task<bool> ImportCsv([FromForm] List<IFormFile> files, [FromQuery] string table, [FromQuery] string comId, [FromQuery] string connKey)
    {
        SetUserContextToServices();
        return _fileService.ImportCsv(files, table, comId, connKey);
    }

    [HttpPost("/api/fileUpload/file")]
    public Task<string> PostFileAsync([FromForm] IFormFile file, bool reup = false)
    {
        SetUserContextToServices();
        return _fileService.PostFileAsync(file, reup);
    }

    [HttpPost("api/[Controller]/Image")]
    public Task<string> PostImageAsync([FromServices] IWebHostEnvironment host,
        [FromQuery] string name = "Captured", [FromQuery] bool reup = false)
    {
        SetUserContextToServices();
        return _fileService.PostImageAsync(host, name, reup);
    }

    [HttpPost("api/fileUpload/deleteFile")]
    public ValueTask<bool> DeleteFile([FromBody] string path)
    {
        return _userSvc.DeleteFile(path);
    }

    [AllowAnonymous]
    [HttpGet("api/dictionary")]
    public Task<Dictionary<string, object>[]> Dictionary()
    {
        return _userSvc.GetDictionary();
    }

    [HttpPost("api/feature/mynotification")]
    public Task<Dictionary<string, object>[]> MyNotification()
    {
        return _userSvc.MyNotification();
    }

    [AllowAnonymous]
    [HttpGet("/api/webConfig")]
    public Task<Dictionary<string, object>[]> WebConfig()
    {
        return _userSvc.WebConfig();
    }

    [HttpPost("api/[Controller]/userSetting")]
    public Task<bool> Dictionary([FromBody] UserSetting userSetting)
    {
        return _userSvc.PostUserSetting(userSetting);
    }

    [HttpPost("/api/feature/go")]
    public Task<SqlResult> Go([FromBody] SqlViewModel entity)
    {
        SetUserContextToServices();
        return _queryService.Go(entity);
    }

    [HttpPost("/api/feature/gos")]
    public Task<Dictionary<string, object>[][]> Gos([FromBody] List<Gos> entitys)
    {
        SetUserContextToServices();
        return _queryService.Gos(entitys);
    }

    [HttpPost("/api/feature/gobyname")]
    public Task<SqlResult> GoByName([FromBody] SqlViewModel entity)
    {
        SetUserContextToServices();
        return _queryService.GoByName(entity);
    }

    [HttpPatch("/api/feature/run")]
    public Task<SqlResult> Run([FromBody] PatchVM entity)
    {
        SetUserContextToServices();
        return _patchService.SavePatch2(entity);
    }

    [HttpPatch("/api/feature/runs")]
    public Task<SqlResult> Runs([FromBody] List<PatchVM> entitys)
    {
        SetUserContextToServices();
        return _patchService.SavePatchs2(entitys);
    }

    [HttpPost("/api/CheckDelete")]
    public async Task<CheckDeleteResult> CheckDelete([FromBody] CheckDeleteItem entity)
    {
        return await _userSvc.CheckDelete(entity);
    }

    [HttpDelete("/api/feature/delete")]
    public Task<bool> Delete([FromBody] PatchVM entity)
    {
        SetUserContextToServices();
        return _patchService.HardDelete(entity);
    }

    [HttpPost("/api/feature/com")]
    public Task<SqlComResult> Com([FromBody] SqlViewModel entity)
    {
        SetUserContextToServices();
        return _queryService.ComQuery(entity);
    }

    [HttpGet("/api/feature/getMenu")]
    public Task<Dictionary<string, object>[]> GetMenu()
    {
        SetUserContextToServices();
        return _metadataService.GetMenu();
    }

    [HttpPost("/api/feature/getFeature")]
    public Feature GetFeature([FromBody] ServiceVM vm)
    {
        SetUserContextToServices();
        return _metadataService.GetFeature(vm.Name);
    }

    [HttpPost("/api/feature/report")]
    public Task<Dictionary<string, object>[][]> Report([FromBody] SqlViewModel entity)
    {
        SetUserContextToServices();
        return _queryService.Report(entity);
    }

    [HttpPost("/api/feature/sql")]
    public Task<Dictionary<string, object>[][]> Sql([FromBody] SqlViewModel entity)
    {
        SetUserContextToServices();
        return _queryService.Sql(entity);
    }

    [HttpPost("api/GetMessageActive")]
    public async Task<Dictionary<string, object>> GetMessageActive()
    {
        return await _userSvc.GetMessageActive();
    }
}
