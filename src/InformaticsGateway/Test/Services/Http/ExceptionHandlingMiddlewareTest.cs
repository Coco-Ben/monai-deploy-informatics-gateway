﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class ExceptionHandlingMiddlewareTest
    {
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _logger;

        public ExceptionHandlingMiddlewareTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        }

        [Fact(DisplayName = "InvokeAsync - Proceeeds wit next request")]
        public async Task InvokeAsync_ProceedsWithNextRequest()
        {
            var context = new DefaultHttpContext();
            RequestDelegate next = (HttpContext hc) => Task.CompletedTask;

            var middleware = new ExceptionHandlingMiddleware(next, _loggerFactory.Object);
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = new StreamReader(context.Response.Body).ReadToEnd();
            Assert.Empty(body);
        }

        [Fact(DisplayName = "InvokeAsync - Handles Exception")]
        public async Task InvokeAsync_HandlesExcption()
        {
            var context = new DefaultHttpContext();
            RequestDelegate next = (HttpContext hc) => throw new Exception("bad");

            var middleware = new ExceptionHandlingMiddleware(next, _loggerFactory.Object);
            context.Response.Body = new MemoryStream();

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = new StreamReader(context.Response.Body).ReadToEnd();

            Assert.Equal("application/json", context.Response.ContentType);
            Assert.Equal(500, context.Response.StatusCode);

            var problem = JsonConvert.DeserializeObject<ProblemDetails>(body);
            Assert.Equal("bad", problem.Title);
        }
    }
}
