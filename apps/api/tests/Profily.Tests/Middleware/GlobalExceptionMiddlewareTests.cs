using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Profily.Api.Middleware;
using Profily.Core.Exceptions;

namespace Profily.Tests.Middleware;

public sealed class GlobalExceptionMiddlewareTests
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger = NullLogger<GlobalExceptionMiddleware>.Instance;

    private GlobalExceptionMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, _logger);

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task NotFoundException_Returns404WithProblemDetailsAsync()
    {
        var middleware = CreateMiddleware(_ => throw new NotFoundException("Project", Guid.NewGuid()));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrorsAsync()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["title"] = ["Title is required"],
            ["startDate"] = ["Start date must be in the past"]
        };
        var middleware = CreateMiddleware(_ => throw new Core.Exceptions.ValidationException(errors));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal("Validation Error", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task ConflictException_Returns409Async()
    {
        var middleware = CreateMiddleware(_ => throw new ConflictException("Duplicate entry"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(409, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal("Duplicate entry", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ProPlanRequiredException_Returns403Async()
    {
        var middleware = CreateMiddleware(_ => throw new ProPlanRequiredException());
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal("Pro Plan Required", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessageAsync()
    {
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("DB connection failed with secret info"));
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal("An unexpected error occurred", body.RootElement.GetProperty("detail").GetString());
        // Must NOT contain the actual exception message (security)
        Assert.DoesNotContain("secret info", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task NoException_PassesThroughAsync()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Exception_WhenResponseHasStarted_DoesNotWriteBodyAsync()
    {
        // Use a custom HttpContext where HasStarted returns true
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
        context.Response.Body = new MemoryStream();

        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("fail"));

        // Should not throw even though response has started
        await middleware.InvokeAsync(context);

        // Body should be empty — middleware skipped writing
        Assert.Equal(0, context.Response.Body.Length);
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true; // Always report as started
        public void OnCompleted(Func<object, Task> callback, object state) { }
        public void OnStarting(Func<object, Task> callback, object state) { }
    }
}
