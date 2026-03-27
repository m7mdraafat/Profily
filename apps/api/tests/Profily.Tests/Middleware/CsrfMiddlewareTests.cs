using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Profily.Api.Middleware;

namespace Profily.Tests.Middleware;

public sealed class CsrfMiddlewareTests
{
    private const string AllowedOrigin = "https://app.profily.dev";
    private readonly ILogger<CsrfMiddleware> _logger = NullLogger<CsrfMiddleware>.Instance;

    private CsrfMiddleware CreateMiddleware(RequestDelegate? next = null)
        => new(next ?? (_ => Task.CompletedTask), AllowedOrigin, _logger);

    [Fact]
    public async Task GetRequest_PassesThroughWithoutOriginCheckAsync()
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, AllowedOrigin, _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Origin = "https://evil.com";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task PostWithCorrectOrigin_PassesThroughAsync()
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, AllowedOrigin, _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Origin = AllowedOrigin;

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task PostWithWrongOrigin_Returns403Async()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Origin = "https://evil.com";

        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task PostWithNoOrigin_PassesThroughAsync()
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, AllowedOrigin, _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        // No Origin header set

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task WebhookPath_SkipsCsrfCheckAsync()
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, AllowedOrigin, _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/payments/webhook";
        context.Request.Headers.Origin = "https://paymob.com";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    public async Task MutatingMethodsWithWrongOrigin_Return403Async(string method)
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Headers.Origin = "https://evil.com";

        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task WebhookPath_CaseInsensitive_SkipsCsrfCheckAsync()
    {
        var nextCalled = false;
        var middleware = new CsrfMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, AllowedOrigin, _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/API/Payments/Webhook";
        context.Request.Headers.Origin = "https://paymob.com";

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }
}
