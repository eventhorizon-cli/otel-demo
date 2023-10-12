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

builder.Services.AddHttpClient();
builder.Services.AddOpenTelemetry()
    // 这边配置的 Resource 是全局的，Log、Metric、Trace 都会使用这个 Resource
    .ConfigureResource(resourceBuilder =>
    {
        resourceBuilder
            .AddService("FooService", "TestNamespace", "1.0.0")
            .AddTelemetrySdk();
    })
    .WithTracing(tracerBuilder =>
    {
        tracerBuilder
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter =
                    httpContent => httpContent.Request.Path.StartsWithSegments("/swagger") == false;
            })
            .AddHttpClientInstrumentation()
            .AddSource("FooActivitySource")
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:8200"));
    }).WithMetrics(meterBuilder =>
    {
        meterBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("FooMeter")
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpLogging();

app.MapControllers();

app.Run();