using Core.Exceptions;
using Core.Extensions;
using Core.Middlewares;
using Core.Models;
using Core.Services;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Models;
using CoreAPI.Services;
using CoreAPI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static OpenAIHttpClientService;
using System.Text;

namespace Core.Controllers;

[Authorize]
public class UserController(UserService _userSvc, PdfService _pdfService, ExcelService _excelService, WebSocketService socketSvc, OpenAIHttpClientService _openAIHttpClientService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("/api/auth/login")]
    public async Task<ActionResult<Token>> SignInAsync([FromBody] LoginVM login)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return await _userSvc.SignInAsync(login);
    }

    [HttpPost("/api/CreateUser")]
    public async Task<Partner> CreateUser([FromBody] Partner entity)
    {
        return await _userSvc.CreateUser(entity);
    }

    [AllowAnonymous]
    [HttpPost("api/[Controller]/SignOut")]
    public Task<bool> SignOutAsync([FromBody] Token token)
    {
        return _userSvc.SignOutAsync(token);
    }

    [HttpPost("api/StartSchedule")]
    public async Task<PlanEmail> StartSchedule([FromBody] PlanEmail token)
    {
        return await _userSvc.CreateSchedule(token);
    }

    [HttpPost("api/PauseSchedule")]
    public async Task<PlanEmail> PauseSchedule([FromBody] PlanEmail token)
    {
        return await _userSvc.PauseSchedule(token);
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

    [AllowAnonymous]
    [HttpPost("/api/auth/refreshToken")]
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

    [HttpPost("api/[Controller]/UpdatePassword")]
    public Task<bool> UpdatePassword([FromBody] UpdatePasswordVM login)
    {
        return _userSvc.UpdatePassword(login);
    }

    [HttpPost("/api/GenPdf")]
    public async Task<string> HtmlToPdf([FromBody] PdfVM vm)
    {
        return await _pdfService.HtmlToPdf(vm);
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

    [AllowAnonymous]
    [HttpPost("api/[Controller]/comp")]
    public async Task<object> LoadComponent([FromBody] SqlViewModel vm)
    {
        var res = await _userSvc.LoadComponent(vm);
        return res;
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

    [HttpPost("/api/fileUpload/file")]
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

    [AllowAnonymous]
    [HttpGet("api/exchangeRate")]
    public Task<Dictionary<string, object>[]> ExchangeRate()
    {
        return _userSvc.GetExchangeRate();
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

    [AllowAnonymous]
    [HttpGet("/api/salesFunction")]
    public Task<Dictionary<string, object>[]> SalesFunction()
    {
        return _userSvc.SalesFunction();
    }

    [HttpPost("api/userSetting")]
    public Task<bool> Dictionary([FromBody] UserSetting userSetting)
    {
        return _userSvc.PostUserSetting(userSetting);
    }

    [HttpPost("/api/feature/notificationuser")]
    public Task<bool> NotificationUser([FromBody] NotificationVM entity)
    {
        return _userSvc.NotificationUser(entity);
    }

    [HttpPost("/api/feature/go")]
    public Task<SqlResult> Go([FromBody] SqlViewModel entity)
    {
        return _userSvc.Go(entity);
    }

    [HttpPost("/api/feature/gos")]
    public Task<Dictionary<string, object>[][]> Gos([FromBody] List<Gos> entitys)
    {
        return _userSvc.Gos(entitys);
    }

    [HttpPost("/api/feature/gobyname")]
    public Task<SqlResult> GoByName([FromBody] SqlViewModel entity)
    {
        return _userSvc.GoByName(entity);
    }

    [HttpPost("/api/Conversation")]
    public async Task<Conversation> Conversation([FromBody] Conversation entity)
    {
        return await _userSvc.Conversation(entity);
    }

    [HttpPost("/api/MoveHBL")]
    public async Task<bool> MoveHBL([FromBody] MoveHBLVM entity)
    {
        return await _userSvc.MoveHBL(entity);
    }

    [HttpPost("/api/SplitFee")]
    public async Task<bool> SplitFee([FromBody] FeeVM entity)
    {
        return await _userSvc.SplitFee(entity);
    }

    [HttpPost("/api/AddFee")]
    public async Task<bool> AddFee([FromBody] FeeVM entity)
    {
        return await _userSvc.AddFee(entity);
    }

    [HttpPatch("/api/feature/run")]
    public Task<SqlResult> Run([FromBody] PatchVM entity)
    {
        return _userSvc.SavePatch2(entity);
    }

    [HttpPost("/api/feature/AsyncTo/{t}/{featureName}")]
    public async Task<bool> AsyncTo([FromRoute] string t, [FromRoute] string featureName)
    {
        return await _userSvc.AsyncTo(t, featureName);
    }

    [HttpPatch("/api/feature/runs")]
    public Task<SqlResult> Runs([FromBody] List<PatchVM> entitys)
    {
        return _userSvc.SavePatchs2(entitys);
    }

    [HttpPost("/api/feature/SendEntity")]
    public Task<SqlResult> SendEntity([FromBody] PatchVM entity)
    {
        return _userSvc.SendEntity(entity);
    }

    [HttpPost("/api/feature/ApprovedEntity")]
    public Task<SqlResult> ApprovedEntity([FromBody] PatchVM entity)
    {
        return _userSvc.ApprovedEntity(entity);
    }

    [HttpPost("/api/feature/ForwardEntity")]
    public Task<SqlResult> ForwardEntity([FromBody] PatchVM entity)
    {
        return _userSvc.ForwardEntity(entity);
    }

    [HttpPost("/api/feature/DeclineEntity")]
    public Task<SqlResult> DeclineEntity([FromBody] PatchVM entity)
    {
        return _userSvc.DeclineEntity(entity);
    }

    [HttpPost("/api/CheckDelete")]
    public async Task<CheckDeleteResult> CheckDelete([FromBody] CheckDeleteItem entity)
    {
        return await _userSvc.CheckDelete(entity);
    }

    [HttpDelete("/api/feature/delete")]
    public Task<bool> Delete([FromBody] PatchVM entity)
    {
        return _userSvc.HardDelete(entity);
    }

    [HttpPost("/api/feature/com")]
    public Task<SqlComResult> Com([FromBody] SqlViewModel entity)
    {
        return _userSvc.ComQuery(entity);
    }

    [HttpPost("/api/feature/getService")]
    public Task<Dictionary<string, object>[][]> GetService([FromBody] ServiceVM vm)
    {
        return _userSvc.ReadDs($"Select * from Component where FieldName = N'{vm.Name}' and ComponentType = 'Service'", null);
    }

    [AllowAnonymous]
    [HttpGet("/api/feature/getMenu")]
    public Task<Dictionary<string, object>[]> GetMenu()
    {
        return _userSvc.GetMenu();
    }

    [AllowAnonymous]
    [HttpGet("/api/feature/PublishAllFeature/{t}")]
    public Task<bool> PublishAllFeature([FromRoute] string t)
    {
        return _userSvc.PublishAllFeature(t);
    }

    [HttpPost("/api/feature/getFeature")]
    public Task<Feature> GetFeature([FromBody] ServiceVM vm)
    {
        return _userSvc.GetFeature(vm.Name);
    }

    [HttpPost("/api/chat")]
    public Task<Chat> CreateAsync([FromBody] Chat entity)
    {
        return _userSvc.Chat(entity);
    }

    [HttpPost("/api/feature/report")]
    public Task<Dictionary<string, object>[][]> Report([FromBody] SqlViewModel entity)
    {
        return _userSvc.Report(entity);
    }

    [HttpPost("/api/feature/sql")]
    public Task<Dictionary<string, object>[][]> Sql([FromBody] SqlViewModel entity)
    {
        return _userSvc.Sql(entity);
    }

    [HttpPost("api/GetUserActive")]
    public async Task<User[]> GetUserActive()
    {
        return await _userSvc.GetUserActive();
    }

    [HttpPost("api/GetMessageActive")]
    public async Task<Dictionary<string, object>> GetMessageActive()
    {
        return await _userSvc.GetMessageActive();
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

    [HttpPost("api/[Controller]/cmd")]
    public string Cmdline([FromBody] Cmd cmd)
    {
        if (!_userSvc.TenantCode.Equals("System", StringComparison.OrdinalIgnoreCase)
            || !_userSvc.RoleNames.Any(x => x.Equals("System", StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("Must login with system tenant and system role");

        return _userSvc.CommandOutput(cmd);
    }
}