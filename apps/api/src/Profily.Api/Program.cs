using Profily.Api.Endpoints;
using Profily.Api.Middleware;
using Profily.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (includes DbContext, settings, auth services)
builder.Services.AddInfrastructure(builder.Configuration);

// CORS
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Profily", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware Pipeline (order matters)
app.UseMiddleware<GlobalExceptionMiddleware>();   // Must be first — catches all unhandled exceptions
app.UseCors("Profily");
app.UseMiddleware<CsrfMiddleware>(frontendUrl);
app.UseMiddleware<SessionAuthMiddleware>();

// Endpoints
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapAuthEndpoints();

app.Run();