using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebPageAnalyzer.Api.Models.Requests;
using WebPageAnalyzer.Api.Models.Responses;
using WebPageAnalyzer.Api.Services;
using WebPageAnalyzer.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Завершаем запуск сразу, если без обязательной конфигурации работа приложения невозможна.
var connectionString =
    builder.Configuration.GetConnectionString("PostgreSql");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string is not configured.");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.SnakeCaseLower;

        options.JsonSerializerOptions.WriteIndented = true;
    });

// Ошибки привязки модели возвращаются в том же контракте, что и ошибки анализа.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        string errorMessage = context.ModelState
            .SelectMany(x => x.Value!.Errors)
            .Select(x => x.ErrorMessage)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
            ?? "Request body is invalid.";

        return new BadRequestObjectResult(
            new AnalyzeResponse
            {
                IsError = 1,
                ErrorCode = "INVALID_REQUEST_BODY",
                ErrorMessage = errorMessage
            });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<
    IValidator<AnalyzeRequest>,
    AnalyzeRequestValidator>();

builder.Services.AddScoped<
    IPageAnalyzerService,
    PageAnalyzerService>();

var app = builder.Build();

app.UseSwagger(options =>
{
    options.RouteTemplate =
        "api/swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";

    options.SwaggerEndpoint(
        "/api/swagger/v1/swagger.json",
        "WebPageAnalyzer API v1");
});

app.MapGet("/", () =>
        Results.Redirect("/api/swagger"))
    .ExcludeFromDescription();

app.MapControllers();

app.Run();
