﻿// Copyright 2021 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class SourceAeTitleControllerTest
    {
        private SourceAeTitleController _controller;
        private Mock<ProblemDetailsFactory> _problemDetailsFactory;
        private Mock<ILogger<SourceAeTitleController>> _logger;
        private Mock<ILogger<ConfigurationValidator>> _validationLogger;
        private Mock<IInformaticsGatewayRepository<SourceApplicationEntity>> _repository;

        public SourceAeTitleControllerTest()
        {
            _logger = new Mock<ILogger<SourceAeTitleController>>();
            _validationLogger = new Mock<ILogger<ConfigurationValidator>>();

            _problemDetailsFactory = new Mock<ProblemDetailsFactory>();
            _problemDetailsFactory.Setup(_ => _.CreateProblemDetails(
                    It.IsAny<HttpContext>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>())
                )
                .Returns((HttpContext httpContext, int? statusCode, string title, string type, string detail, string instance) =>
                {
                    return new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Type = type,
                        Detail = detail,
                        Instance = instance
                    };
                });

            _repository = new Mock<IInformaticsGatewayRepository<SourceApplicationEntity>>();

            _controller = new SourceAeTitleController(
                 _logger.Object,
                 _repository.Object)
            {
                ProblemDetailsFactory = _problemDetailsFactory.Object
            };
        }

        #region Get

        [Fact(DisplayName = "Get - Shall return available source AETs")]
        public async void Get_ShallReturnAllSourceAets()
        {
            var data = new List<SourceApplicationEntity>();
            for (int i = 1; i <= 5; i++)
            {
                data.Add(new SourceApplicationEntity()
                {
                    AeTitle = $"AET{i}",
                    HostIp = "host",
                    Name = $"AET{i}",
                });
            }

            _repository.Setup(p => p.ToListAsync()).Returns(Task.FromResult(data));

            var result = await _controller.Get();
            Assert.Equal(data.Count, result.Value.Count());
            _repository.Verify(p => p.ToListAsync(), Times.Once());
        }

        [Fact(DisplayName = "Get - Shall return problem on failure")]
        public async void Get_ShallReturnProblemOnFailure()
        {
            _repository.Setup(p => p.ToListAsync()).Throws(new Exception("error"));

            var result = await _controller.Get();
            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying database.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        }

        #endregion Get

        #region GetAeTitle

        [Fact(DisplayName = "GetAeTitle - Shall return matching object")]
        public async void GetAeTitle_ReturnsAMatch()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(
                Task.FromResult(
                new SourceApplicationEntity
                {
                    AeTitle = value,
                    HostIp = "host",
                    Name = value,
                }));

            var result = await _controller.GetAeTitle(value);
            Assert.NotNull(result.Value);
            Assert.Equal(value, result.Value.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [Fact(DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async void GetAeTitle_Returns404IfNotFound()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(SourceApplicationEntity)));

            var result = await _controller.GetAeTitle(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [Fact(DisplayName = "GetAeTitle - Shall return problem on failure")]
        public async void GetAeTitle_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Throws(new Exception("error"));

            var result = await _controller.GetAeTitle(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error querying Source Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion GetAeTitle

        #region Create

        [Fact(DisplayName = "GetAeTitle - Shall return problem on validation failure")]
        public async void Create_ShallReturnBadRequestWithBadJobProcessType()
        {
            var aeTitle = "TOOOOOOOOOOOOOOOOOOOOOOOLONG";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Validation error.", problem.Title);
            Assert.Equal($"'{aeTitle}' is not a valid AE Title (source: SourceApplicationEntity).", problem.Detail);
            Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
        }

        [Fact(DisplayName = "Create - Shall return problem if failed to add")]
        public async void Create_ShallReturnBadRequestOnAddFailure()
        {
            var aeTitle = "AET";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>())).Throws(new Exception("error"));

            var result = await _controller.Create(aeTitles);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error adding new Source Application Entity.", problem.Title);
            Assert.Equal($"error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);

            _repository.Verify(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "Create - Shall return CreatedAtAction")]
        public async void Create_ShallReturnCreatedAtAction()
        {
            var aeTitle = "AET";
            var aeTitles = new SourceApplicationEntity
            {
                AeTitle = aeTitle,
                HostIp = "host",
                Name = aeTitle,
            };

            _repository.Setup(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Create(aeTitles);

            Assert.IsType<CreatedAtActionResult>(result.Result);

            _repository.Verify(p => p.AddAsync(It.IsAny<SourceApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        #endregion Create

        #region Delete

        [Fact(DisplayName = "GetAeTitle - Shall return deleted object")]
        public async void Delete_ReturnsDeleted()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));

            _repository.Setup(p => p.Remove(It.IsAny<SourceApplicationEntity>()));
            _repository.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var result = await _controller.Delete(value);
            Assert.NotNull(result.Value);
            Assert.Equal(value, result.Value.AeTitle);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
            _repository.Verify(p => p.Remove(entity), Times.Once());
            _repository.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "GetAeTitle - Shall return 404 if not found")]
        public async void Delete_Returns404IfNotFound()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(default(SourceApplicationEntity)));

            var result = await _controller.Delete(value);

            Assert.IsType<NotFoundResult>(result.Result);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        [Fact(DisplayName = "Delete - Shall return problem on failure")]
        public async void Delete_ShallReturnProblemOnFailure()
        {
            var value = "AET";
            var entity = new SourceApplicationEntity
            {
                AeTitle = value,
                HostIp = "host",
                Name = value,
            };
            _repository.Setup(p => p.FindAsync(It.IsAny<string>())).Returns(Task.FromResult(entity));
            _repository.Setup(p => p.Remove(It.IsAny<SourceApplicationEntity>())).Throws(new Exception("error"));

            var result = await _controller.Delete(value);

            var objectResult = result.Result as ObjectResult;
            Assert.NotNull(objectResult);
            var problem = objectResult.Value as ProblemDetails;
            Assert.NotNull(problem);
            Assert.Equal("Error deleting Source Application Entity.", problem.Title);
            Assert.Equal("error", problem.Detail);
            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
            _repository.Verify(p => p.FindAsync(value), Times.Once());
        }

        #endregion Delete
    }
}