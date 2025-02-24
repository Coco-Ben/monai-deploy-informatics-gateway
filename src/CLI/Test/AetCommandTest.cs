/*
 * Copyright 2021-2023 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.CLI.Services;
using Monai.Deploy.InformaticsGateway.Client;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.CLI.Test
{
    public class AetCommandTest
    {
        private readonly Mock<IConfigurationService> _configurationService;
        private readonly CommandLineBuilder _commandLineBuilder;
        private readonly Parser _paser;
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IInformaticsGatewayClient> _informaticsGatewayClient;

        public AetCommandTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _logger = new Mock<ILogger>();
            _configurationService = new Mock<IConfigurationService>();
            _informaticsGatewayClient = new Mock<IInformaticsGatewayClient>();
            _commandLineBuilder = new CommandLineBuilder()
                .UseHost(
                    _ => Host.CreateDefaultBuilder(),
                    host =>
                    {
                        host.ConfigureServices(services =>
                        {
                            services.AddScoped<IConsoleRegion, TestConsoleRegion>();
                            services.AddSingleton<IConsole, TestConsole>();
                            services.AddSingleton<ITerminal, TestTerminal>();
                            services.AddSingleton<ILoggerFactory>(p => _loggerFactory.Object);
                            services.AddSingleton<IInformaticsGatewayClient>(p => _informaticsGatewayClient.Object);
                            services.AddSingleton<IConfigurationService>(p => _configurationService.Object);
                        });
                    });
            _commandLineBuilder.Command.AddCommand(new AetCommand());
            _paser = _commandLineBuilder.Build();

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
            _configurationService.SetupGet(p => p.IsInitialized).Returns(true);
            _configurationService.SetupGet(p => p.IsConfigExists).Returns(true);
            _configurationService.Setup(p => p.Configurations.InformaticsGatewayServerUri).Returns(new Uri("http://test"));
            _configurationService.Setup(p => p.Configurations.InformaticsGatewayServerEndpoint).Returns("http://test");
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact(DisplayName = "aet comand")]
        public async Task Aet_Command()
        {
            var command = "aet";
            var result = _paser.Parse(command);
            Assert.Equal("Required command was not provided.", result.Errors.First().Message);

            int exitCode = await _paser.InvokeAsync(command);
            Assert.Equal(ExitCodes.Success, exitCode);
        }

        [Fact(DisplayName = "aet add comand")]
        public async Task AetAdd_Command()
        {
            var command = "aet add -n MyName -a MyAET --workflows App MyCoolApp TheApp";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new MonaiApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                Workflows = result.CommandResult.Children[2].Tokens.Select(p => p.Value).ToList(),
            };
            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Collection(entity.Workflows,
                item => item.Equals("App"),
                item => item.Equals("MyCoolApp"),
                item => item.Equals("TheApp"));

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Create(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.MonaiScpAeTitle.Create(
                    It.Is<MonaiApplicationEntity>(o => o.AeTitle == entity.AeTitle && o.Name == entity.Name && Enumerable.SequenceEqual(o.Workflows, entity.Workflows)),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet add comand with plug-ins")]
        public async Task AetAdd_Command_WithPlugIns()
        {
            var command = "aet add -n MyName -a MyAET --workflows App MyCoolApp TheApp --plugins \"PlugInTypeA\" \"PlugInTypeB\"";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new MonaiApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                Workflows = result.CommandResult.Children[2].Tokens.Select(p => p.Value).ToList(),
                PlugInAssemblies = result.CommandResult.Children[3].Tokens.Select(p => p.Value).ToList(),
            };
            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Collection(entity.Workflows,
                item => item.Equals("App"),
                item => item.Equals("MyCoolApp"),
                item => item.Equals("TheApp"));
            Assert.Collection(entity.PlugInAssemblies,
                item => item.Equals("PlugInTypeA"),
                item => item.Equals("PlugInTypeB"));

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Create(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.MonaiScpAeTitle.Create(
                    It.Is<MonaiApplicationEntity>(o => o.AeTitle == entity.AeTitle && o.Name == entity.Name && Enumerable.SequenceEqual(o.Workflows, entity.Workflows)),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet add comand with allowed & ignored SOP classes")]
        public async Task AetAdd_Command_AllowedIgnoredSopClasses()
        {
            var command = "aet add -n MyName -a MyAET --workflows App MyCoolApp TheApp -i A B C -s D E F";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new MonaiApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = result.CommandResult.Children[1].Tokens[0].Value,
                Workflows = result.CommandResult.Children[2].Tokens.Select(p => p.Value).ToList(),
                IgnoredSopClasses = result.CommandResult.Children[3].Tokens.Select(p => p.Value).ToList(),
                AllowedSopClasses = result.CommandResult.Children[4].Tokens.Select(p => p.Value).ToList(),
            };
            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Collection(entity.Workflows,
                item => item.Equals("App"),
                item => item.Equals("MyCoolApp"),
                item => item.Equals("TheApp"));
            Assert.Collection(entity.AllowedSopClasses,
                item => item.Equals("D"),
                item => item.Equals("E"),
                item => item.Equals("F"));
            Assert.Collection(entity.IgnoredSopClasses,
                item => item.Equals("A"),
                item => item.Equals("B"),
                item => item.Equals("C"));

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Create(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.MonaiScpAeTitle.Create(
                    It.Is<MonaiApplicationEntity>(o => o.AeTitle == entity.AeTitle &&
                                                        o.Name == entity.Name &&
                                                        Enumerable.SequenceEqual(o.Workflows, entity.Workflows) &&
                                                        Enumerable.SequenceEqual(o.AllowedSopClasses, entity.AllowedSopClasses) &&
                                                        Enumerable.SequenceEqual(o.IgnoredSopClasses, entity.IgnoredSopClasses)),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet add comand exception")]
        public async Task AetAdd_Command_Exception()
        {
            var command = "aet add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Create(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.MonaiScp_ErrorCreate, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.Create(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error creating MONAI SCP AE Title", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet add comand configuration exception")]
        public async Task AetAdd_Command_ConfigurationException()
        {
            var command = "aet add -n MyName -a MyAET --apps App MyCoolApp TheApp";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet remove comand")]
        public async Task AetRemove_Command()
        {
            var command = "aet rm -n MyName";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var name = result.CommandResult.Children[0].Tokens[0].Value;
            Assert.Equal("MyName", name);

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.Delete(It.Is<string>(o => o.Equals(name)), It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet remove comand exception")]
        public async Task AetRemove_Command_Exception()
        {
            var command = "aet rm -n MyName";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.MonaiScp_ErrorDelete, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error deleting MONAI SCP AE Title", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet list comand configuration exception")]
        public async Task AetRemove_Command_ConfigurationException()
        {
            var command = "aet rm -n MyName";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet list comand")]
        public async Task AetList_Command()
        {
            var command = "aet list";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new MonaiApplicationEntity()
            {
                Name = "MyName",
                AeTitle = "MyAET",
                Workflows = new List<string>() { "MyApp1", "MyCoolApp" }
            };

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MonaiApplicationEntity>() { entity });

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet list comand exception")]
        public async Task AetList_Command_Exception()
        {
            var command = "aet list";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.MonaiScp_ErrorList, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error retrieving MONAI SCP AE Titles", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet list comand configuration exception")]
        public async Task AetList_Command_ConfigurationException()
        {
            var command = "aet list";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet list comand empty")]
        public async Task AetList_Command_Empty()
        {
            var command = "aet list";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MonaiApplicationEntity>());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.List(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging("No MONAI SCP Application Entities configured.", LogLevel.Warning, Times.Once());
        }

        [Fact(DisplayName = "aet update command")]
        public async Task AetUpdate_Command()
        {
            var command = "aet update -n MyName --workflows App MyCoolApp TheApp -i A B C -s D E F -p PlugInAssemblyA PlugInAssemblyB";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entity = new MonaiApplicationEntity()
            {
                Name = result.CommandResult.Children[0].Tokens[0].Value,
                AeTitle = "MyAET",
                Workflows = result.CommandResult.Children[1].Tokens.Select(p => p.Value).ToList(),
                IgnoredSopClasses = result.CommandResult.Children[2].Tokens.Select(p => p.Value).ToList(),
                AllowedSopClasses = result.CommandResult.Children[3].Tokens.Select(p => p.Value).ToList(),
                PlugInAssemblies = result.CommandResult.Children[4].Tokens.Select(p => p.Value).ToList(),
            };

            Assert.Equal("MyName", entity.Name);
            Assert.Equal("MyAET", entity.AeTitle);
            Assert.Collection(entity.Workflows,
                item => item.Equals("App"),
                item => item.Equals("MyCoolApp"),
                item => item.Equals("TheApp"));
            Assert.Collection(entity.AllowedSopClasses,
                item => item.Equals("D"),
                item => item.Equals("E"),
                item => item.Equals("F"));
            Assert.Collection(entity.IgnoredSopClasses,
                item => item.Equals("A"),
                item => item.Equals("B"),
                item => item.Equals("C"));
            Assert.Collection(entity.PlugInAssemblies,
                item => item.Equals("PlugInAssemblyA"),
                item => item.Equals("PlugInAssemblyB"));

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Update(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(
                p => p.MonaiScpAeTitle.Update(
                    It.Is<MonaiApplicationEntity>(o => o.Name == entity.Name &&
                                                       o.Timeout == entity.Timeout &&
                                                       o.Grouping == entity.Grouping &&
                                                       Enumerable.SequenceEqual(o.Workflows, entity.Workflows) &&
                                                       Enumerable.SequenceEqual(o.AllowedSopClasses, entity.AllowedSopClasses) &&
                                                       Enumerable.SequenceEqual(o.IgnoredSopClasses, entity.IgnoredSopClasses)),
                    It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet update command exception")]
        public async Task AetUpdate_Command_Exception()
        {
            var command = "aet update -n MyName --workflows App MyCoolApp TheApp -i A B C -s D E F";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.Update(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.MonaiScp_ErrorUpdate, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.Update(It.IsAny<MonaiApplicationEntity>(), It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error updating SCP Application Entity", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet update command configuration exception")]
        public async Task AetUpdate_Command_ConfigurationException()
        {
            var command = "aet update -n MyName --workflows App MyCoolApp TheApp -i A B C -s D E F";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.DicomDestinations.List(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet plugins comand")]
        public async Task AetPlugIns_Command()
        {
            var command = "aet plugins";
            var result = _paser.Parse(command);
            Assert.Equal(ExitCodes.Success, result.Errors.Count);

            var entries = new Dictionary<string, string> { { "A", "1" }, { "B", "2" } };

            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()))
                .ReturnsAsync(entries);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact(DisplayName = "aet plugins comand exception")]
        public async Task AetPlugIns_Command_Exception()
        {
            var command = "aet plugins";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()))
                .Throws(new Exception("error"));

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.MonaiScp_ErrorPlugIns, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLoggingMessageBeginsWith("Error retrieving data input plug-ins", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet plugins comand configuration exception")]
        public async Task AetPlugIns_Command_ConfigurationException()
        {
            var command = "aet plugins";
            _configurationService.SetupGet(p => p.IsInitialized).Returns(false);

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Config_NotConfigured, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Never());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()), Times.Never());

            _logger.VerifyLoggingMessageBeginsWith("Please execute `testhost config init` to intialize Informatics Gateway.", LogLevel.Critical, Times.Once());
        }

        [Fact(DisplayName = "aet plugins comand empty")]
        public async Task AetPlugIns_Command_Empty()
        {
            var command = "aet plugins";
            _informaticsGatewayClient.Setup(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, string>());

            int exitCode = await _paser.InvokeAsync(command);

            Assert.Equal(ExitCodes.Success, exitCode);

            _informaticsGatewayClient.Verify(p => p.ConfigureServiceUris(It.IsAny<Uri>()), Times.Once());
            _informaticsGatewayClient.Verify(p => p.MonaiScpAeTitle.PlugIns(It.IsAny<CancellationToken>()), Times.Once());

            _logger.VerifyLogging("No MONAI SCP Application Entities configured.", LogLevel.Warning, Times.Once());
        }
    }
}
