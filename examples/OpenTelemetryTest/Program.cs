using FreeRedis;
using FreeRedis.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//redis
var redisClient = new RedisClient(builder.Configuration.GetConnectionString("Redis"));
redisClient.Serialize = obj => System.Text.Json.JsonSerializer.Serialize(obj);
redisClient.Deserialize = (json, type) => System.Text.Json.JsonSerializer.Deserialize(json, type);
//redisClient.Notice += (s, e) =>
//{
//    Console.WriteLine(e.Log);
//};
services.TryAddSingleton<IRedisClient>(redisClient);

//OpenTelemetry
var otel = services.AddOpenTelemetry();
otel.ConfigureResource(resource =>
{
    resource.AddTelemetrySdk();
    resource.AddEnvironmentVariableDetector();
    resource.AddService("FreeRedisTest");
});
var otlpUrl = builder.Configuration["OpenTelemetry:OtlpHttpUrl"];
otel.WithTracing(tracing => tracing.AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddFreeRedisInstrumentation(redisClient)
    .SetSampler(new AlwaysOnSampler())
    //.AddConsoleExporter()
    .AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri($"http://{otlpUrl}/v1/traces");
        otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
        otlpOptions.Headers = "Authorization=Basic YWRtaW46dGVzdEAxMjM=";
    })
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();