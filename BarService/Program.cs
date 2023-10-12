using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.RequestBody |
                            HttpLoggingFields.ResponseBody;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
            .AddService("BarService", "TestNamespace", "1.0.0")
            .AddTelemetrySdk();
    })
    .WithTracing(options =>
    {
        options
            .AddAspNetCoreInstrumentation(options =>
            {
                // 配置 Filter，忽略 swagger 的请求
                options.Filter =
                    httpContent => httpContent.Request.Path.StartsWithSegments("/swagger") == false;
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri("http://localhost:8200"));
    }).WithMetrics(options =>
    {
        options
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri("http://localhost:8200"));
    });

builder.Services.AddLogging(loggingBuilder =>   
{
    loggingBuilder.AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri("http://localhost:8200"));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.MapControllers();

app.Run();