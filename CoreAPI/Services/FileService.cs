using Core.Exceptions;
using Core.ViewModels;
using CoreAPI.BgService;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;

namespace CoreAPI.Services;

public class FileService : IFileService
{
    private readonly ISqlProvider _sql;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _host;
    private readonly ILogger<FileService> _logger;

    // User context
    public string UserId { get; set; }
    public string TenantCode { get; set; }

    public FileService(
        ISqlProvider sql,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IWebHostEnvironment host,
        ILogger<FileService> logger)
    {
        _sql = sql;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _host = host;
        _logger = logger;
    }

    public void SetUserContext(string userId, string tenantCode)
    {
        UserId = userId;
        TenantCode = tenantCode;
    }

    public async Task<string> PostImageAsync(IWebHostEnvironment host,
        string name = "Captured", bool reup = false)
    {
        // This requires access to HttpContext which should be passed in
        // Simplified implementation
        var fileName = $"{Path.GetFileNameWithoutExtension(name)}{Path.GetExtension(name)}";
        var path = GetUploadPath(fileName, host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;

        // Read from request body - requires IHttpContextAccessor
        // This is a placeholder - actual implementation would read the request body
        return GetHttpPath(path, host.WebRootPath);
    }

    public async Task<string> PostFileAsync(IFormFile file, bool reup = false)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
        var path = GetUploadPath(fileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = reup ? IncreaseFileName(path) : path;

        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();

        return GetHttpPath(path, _host.WebRootPath);
    }

    public async Task<bool> ImportCsv(List<IFormFile> files, string table, string comId, string connKey)
    {
        if (string.IsNullOrWhiteSpace(comId) || string.IsNullOrWhiteSpace(table))
        {
            throw new ApiException("ComId or table cannot be null")
            {
                StatusCode = Core.Enums.HttpStatusCode.BadRequest
            };
        }

        if (files == null || files.Count == 0)
        {
            throw new ApiException("No file uploaded")
            {
                StatusCode = Core.Enums.HttpStatusCode.BadRequest
            };
        }

        // Get component - simplified
        var connStr = _sql.GetConnStrFromKey(BgExt.GetConnectionString(_serviceProvider, _configuration, "logistics"));

        var file = files.First();
        var path = GetUploadPath(file.FileName, _host.WebRootPath);
        EnsureDirectoryExist(path);
        path = IncreaseFileName(path);

        using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        stream.Close();

        var patches = await ParseCsvFile(path, table);

        if (patches == null || patches.Count == 0)
        {
            return false;
        }

        using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(connStr);
        await sqlConnection.OpenAsync();
        using var transaction = sqlConnection.BeginTransaction();

        try
        {
            using var command = new Microsoft.Data.SqlClient.SqlCommand();
            command.Transaction = transaction;
            command.Connection = sqlConnection;

            foreach (var patch in patches)
            {
                ImportItem(patch, command, patches.IndexOf(patch));
            }

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return true;
    }

    public string GetUploadPath(string fileName, string webRootPath)
    {
        return Path.Combine(webRootPath, "upload", TenantCode ?? "default", "file", $"U{UserId ?? "system"}", fileName);
    }

    public string IncreaseFileName(string path)
    {
        var uploadedPath = path;
        var index = 0;
        while (File.Exists(path))
        {
            var noExtension = Path.GetFileNameWithoutExtension(uploadedPath);
            var dir = Path.GetDirectoryName(uploadedPath);
            index++;
            path = Path.Combine(dir, $"{noExtension}_{index}{Path.GetExtension(uploadedPath)}");
        }
        return path;
    }

    public void EnsureDirectoryExist(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public string GetHttpPath(string path, string webRootPath)
    {
        var relativePath = path.Replace(webRootPath, "").Replace("\\", "/");
        if (!relativePath.StartsWith("/"))
        {
            relativePath = "/" + relativePath;
        }
        return relativePath;
    }

    private async Task<List<PatchVM>> ParseCsvFile(string path, string table)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var tempPath = IncreaseFileName(path);
        using var streamReader = new StreamReader(path);
        using var streamWriter = new StreamWriter(tempPath);

        string currentLine;
        var lineCount = 0;
        string[] headers = null;
        var patches = new List<PatchVM>();

        while ((currentLine = await streamReader.ReadLineAsync()) != null)
        {
            if (lineCount == 0 || string.IsNullOrWhiteSpace(currentLine))
            {
                lineCount++;
                var firstLine = ParseCsvLine(currentLine, lineCount);
                if (firstLine == null)
                {
                    throw new ApiException("Header must be the first line of csv")
                    {
                        StatusCode = Core.Enums.HttpStatusCode.BadRequest
                    };
                }
                headers = firstLine.Select(x => x.Field).ToArray();
                continue;
            }

            var updatedLine = ParseCsvLine(currentLine, lineCount);
            if (updatedLine != null && headers != null)
            {
                for (int i = 0; i < updatedLine.Count && i < headers.Length; i++)
                {
                    updatedLine[i].Field = headers[i];
                }
                patches.Add(new PatchVM { Table = table, Changes = updatedLine });
            }
            lineCount++;
        }

        return patches;
    }

    private static List<PatchDetail> ParseCsvLine(string currentLine, int lineCount)
    {
        if (string.IsNullOrWhiteSpace(currentLine))
            return null;

        var values = currentLine.Split(',');
        var result = new List<PatchDetail>(values.Length);

        for (int i = 0; i < values.Length; i++)
        {
            result.Add(new PatchDetail
            {
                Field = $"Column{i}",
                Value = values[i].Trim().Trim('"')
            });
        }

        return result;
    }

    private void ImportItem(PatchVM vm, Microsoft.Data.SqlClient.SqlCommand command, int index)
    {
        var idField = vm.Changes.FirstOrDefault(x => x.Field == "Id");
        var valueFields = vm.Changes.Where(x =>
            x.Field.ToLower() != "id" &&
            x.Field.ToLower() != "insertedby" &&
            x.Field.ToLower() != "inserteddate" &&
            x.Field.ToLower() != "updatedby" &&
            x.Field.ToLower() != "updateddate").ToList();

        var update = string.Join(", ", valueFields.Select(x => $"[{x.Field}] = @{x.Field}{index}"));
        var columns = string.Join(", ", valueFields.Select(x => $"[{x.Field}]"));
        var values = string.Join(", ", valueFields.Select(x => $"@{x.Field}{index}"));

        if (command.CommandText.Length > 0)
        {
            command.CommandText += "; ";
        }

        if (idField?.OldVal == null)
        {
            // Insert
            command.CommandText += $"INSERT INTO [{vm.Table}]({columns}) VALUES({values})";
        }
        else
        {
            // Update
            command.CommandText += $"UPDATE [{vm.Table}] SET {update} WHERE Id = '{idField.OldVal}'";
        }

        foreach (var item in valueFields)
        {
            command.Parameters.AddWithValue($"@{item.Field}{index}", item.Value ?? (object)DBNull.Value);
        }
    }
}
