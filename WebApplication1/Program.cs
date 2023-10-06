using Microsoft.AspNetCore.HttpLogging;
using OpenTelemetry.Exporter;
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

builder.Services.AddHttpClient();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
            .AddService("FooService", "TestNamespace", "1.0.0")
            .AddTelemetrySdk();
    })
    .WithTracing(tracerBuilder =>
    {
        tracerBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:8200"))
            .AddJaegerExporter(options =>
            {
                options.Protocol = JaegerExportProtocol.HttpBinaryThrift;
                options.AgentHost = "localhost";
                options.AgentPort = 14268;
            });
    }).WithMetrics(meterBuilder =>
    {
        meterBuilder
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder
                    .AddService("FooService", "TestNamespace", "1.0.0")
                    .AddTelemetrySdk();
            })
            .AddMeter("FooMeter")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter()
            .AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri("http://localhost:8200"));
    });

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:8200"));
    });
});

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.MapControllers();

app.Run();