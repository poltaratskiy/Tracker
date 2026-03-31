using Microsoft.AspNetCore.Http;
using Shouldly;
using Tracker.Dotnet.Libs.RequestContextAccessor;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.Tests.RequestContext;

[TestFixture]
public class RequestContextTests
{
    [Test]
    public async Task Invoke_Should_Set_RefId_And_JwtToken_From_Headers_And_Clear_After_Request()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["RefId"] = "ref-123";
        httpContext.Request.Headers.Authorization = "Bearer jwt-token-123";

        var accessor = new RequestContextAccessor.RequestContextAccessor();

        IRequestContext? contextInsideNext = null;

        var middleware = new RequestContextMiddleware(nextContext =>
        {
            contextInsideNext = accessor.Current;
            return Task.CompletedTask;
        });

        // Act
        await middleware.Invoke(httpContext, accessor);

        // Assert
        contextInsideNext.ShouldNotBeNull();
        contextInsideNext.RefId.ShouldBe("ref-123");
        contextInsideNext.JwtToken.ShouldBe("jwt-token-123");

        accessor.Current.ShouldBeNull();
    }

    [Test]
    public async Task Invoke_Should_Generate_RefId_When_Header_Is_Missing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new RequestContextAccessor.RequestContextAccessor();

        IRequestContext? contextInsideNext = null;

        var middleware = new RequestContextMiddleware(nextContext =>
        {
            contextInsideNext = accessor.Current;
            return Task.CompletedTask;
        });

        // Act
        await middleware.Invoke(httpContext, accessor);

        // Assert
        contextInsideNext.ShouldNotBeNull();
        contextInsideNext.RefId.ShouldNotBeNull();
        contextInsideNext.RefId.Length.ShouldBe(6);
        contextInsideNext.JwtToken.ShouldBeNull();

        accessor.Current.ShouldBeNull();
    }

}
