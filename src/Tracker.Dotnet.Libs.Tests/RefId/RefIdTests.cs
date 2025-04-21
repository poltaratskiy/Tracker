using Microsoft.AspNetCore.Http;
using Shouldly;
using Tracker.Dotnet.Libs.RefId;

namespace Tracker.Dotnet.Libs.Tests.RefId
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public async Task GetRefId_NotEmpty_Should_TakeFromHeader()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["refid"] = "abc123";

            string? resultedRefId = null;
            var middleware = new RefIdMiddleware(ctx =>
            {
                resultedRefId = ctx.Items["RefId"]?.ToString();
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            resultedRefId.ShouldNotBeNull();
            resultedRefId.ShouldBe("abc123");
        }

        [Test]
        public async Task GetRefId_Empty_Should_BeGenerated()
        {
            var context = new DefaultHttpContext();

            string? resultedRefId = null;
            var middleware = new RefIdMiddleware(ctx =>
            {
                resultedRefId = ctx.Items["RefId"]?.ToString();
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            resultedRefId.ShouldNotBeNull();
            resultedRefId.Length.ShouldBe(6);
        }
    }
}