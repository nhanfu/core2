var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedisContainer("Redis");
builder.AddProject<Projects.CoreAPI>("api").WithReference(cache);
builder.Build().Run();
