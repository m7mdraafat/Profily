using Profily.Api.Endpoints;
using Profily.Api.Middleware;
using Profily.Infrastructure.Extensions;
using Serilog;

// Bootstrap logger (captures startup errors)
Log.Logger = new LoggerConfiguration()
	.CreateBootstrapLogger();

try {
	var builder = WebApplication.CreateBuilder(args);

	// Serilog: sinks are config-driven per environment (Dev=Seq, Staging/Prod=AppInsights (future))
	builder.Host.UseSerilog((context, services, config) => config
		.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext());
	
	// Add services to the container.
	builder.Services.AddInfrastructureServices(builder.Configuration);
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();

	var app = builder.Build();

	// Configure the HTTP request pipeline.
	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	// Security middlewares
	app.UseHttpsRedirection();
	app.UseCors(CorsExtensions.PolicyName);
	app.UseAuthentication();
	app.UseAuthorization();

	// Wide Event middleware (after auth so user context is available)
	app.UseMiddleware<WideEventMiddleware>();

	app.MapAuthEndpoints();
	app.MapGitHubEndpoints();
	app.MapTechStackEndpoints();
	
	app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
		.WithName("Health")
		.WithOpenApi();

	app.Run();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Application start-up failed unexpectedly");
}
finally
{
	Log.CloseAndFlush();
}
