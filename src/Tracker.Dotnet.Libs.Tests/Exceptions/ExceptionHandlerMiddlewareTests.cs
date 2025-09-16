using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Extensions.Logging;
using Shouldly;
using System.Text.Json;
using Tracker.Dotnet.Libs.Exceptions;

namespace Tracker.Dotnet.Libs.Tests.Exceptions;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

[TestFixture]
public class ExceptionHandlerMiddlewareTests
{
    private static async Task<ProblemDetails> ReadJsonResponse(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    private static ILogger<ExceptionHandlerMiddleware> GetLogger()
    {
        var loggerConfig = new LoggerConfiguration()
           .MinimumLevel.Verbose()
           .WriteTo.NUnitOutput()
           .CreateLogger();

        var factory = new SerilogLoggerFactory(loggerConfig);
        return factory.CreateLogger<ExceptionHandlerMiddleware>();
    }

    [Test]
    public async Task Should_Invoke_Next_If_No_Exception()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var middleware = new ExceptionHandlerMiddleware(nextMock.Object, GetLogger());

        // Act
        await middleware.Invoke(context);

        // Assert
        nextMock.Verify(x => x(context), Times.Once);
    }

    [Test]
    public async Task Should_Handle_WrongCredentialsException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(new WrongCredentialsException());

        var middleware = new ExceptionHandlerMiddleware(nextMock.Object, GetLogger());

        // Act
        await middleware.Invoke(context);
        var response = await ReadJsonResponse(context);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        response.Title.ShouldBe("Incorrect login or password");
    }

    [Test]
    public async Task Should_Handle_Generic_Exception()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(new Exception("Something went wrong"));

        var middleware = new ExceptionHandlerMiddleware(nextMock.Object, GetLogger());

        // Act
        await middleware.Invoke(context);
        var response = await ReadJsonResponse(context);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        response.Title.ShouldBe("An error occurred while processing your request.");
    }

    [Test]
    public async Task Should_Handle_ApiException_And_Return_Validation_Response()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new ApiException("Validation failed");

        var nextMock = new Mock<RequestDelegate>();
        nextMock.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        var middleware = new ExceptionHandlerMiddleware(nextMock.Object, GetLogger());

        // Act
        await middleware.Invoke(context);
        var response = await ReadJsonResponse(context);

        // Assert
        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        response.Title.ShouldBe("One or more validation errors occurred.");
        response.Detail.ShouldBe("Validation failed");
    }
}

