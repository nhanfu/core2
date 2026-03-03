using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;

namespace CoreAPI.Services.Supabase;

/// <summary>
/// Service for Supabase integration including PostgreSQL, Storage, and Authentication
/// </summary>
public class SupabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseService> _logger;
    private readonly Client _client;
    private readonly string _connectionString;

    public SupabaseService(
        IConfiguration configuration,
        ILogger<SupabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var supabaseUrl = _configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url configuration is required");
        var supabaseKey = _configuration["Supabase:Key"]
            ?? throw new InvalidOperationException("Supabase:Key configuration is required");

        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        _client = new Client(supabaseUrl, supabaseKey, options);

        // Build PostgreSQL connection string
        var dbHost = _configuration["Supabase:DbHost"] ?? "db.dgifkbbufcayhbteebby.supabase.co";
        var dbPort = _configuration["Supabase:DbPort"] ?? "5432";
        var dbName = _configuration["Supabase:DbName"] ?? "postgres";
        var dbUser = _configuration["Supabase:DbUser"] ?? "postgres";
        var dbPassword = _configuration["Supabase:DbPassword"] ?? throw new InvalidOperationException("Supabase:DbPassword is required");

        _connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
    }

    /// <summary>
    /// Gets the Supabase client for storage and auth operations
    /// </summary>
    public Client Client => _client;

    /// <summary>
    /// Gets the PostgreSQL connection string
    /// </summary>
    public string ConnectionString => _connectionString;

    /// <summary>
    /// Gets the Supabase project URL
    /// </summary>
    public string SupabaseUrl => _configuration["Supabase:Url"];

    /// <summary>
    /// Gets the Supabase anon key for client-side operations
    /// </summary>
    public string SupabaseAnonKey => _configuration["Supabase:AnonKey"];

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    public async Task<dynamic> SignInAsync(string email, string password)
    {
        var session = await _client.Auth.SignIn(email, password);
        _logger.LogInformation("User signed in: {Email}", email);
        return session;
    }

    /// <summary>
    /// Sign up new user with email and password
    /// </summary>
    public async Task<dynamic> SignUpAsync(string email, string password)
    {
        var session = await _client.Auth.SignUp(email, password);
        _logger.LogInformation("User signed up: {Email}", email);
        return session;
    }

    /// <summary>
    /// Sign out current user
    /// </summary>
    public async Task SignOut()
    {
        await _client.Auth.SignOut();
        _logger.LogInformation("User signed out");
    }

    /// <summary>
    /// Get current session
    /// </summary>
    public dynamic GetCurrentSession()
    {
        return _client.Auth.CurrentSession;
    }
}
