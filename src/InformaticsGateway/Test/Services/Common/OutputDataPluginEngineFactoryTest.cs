﻿/*
 * Copyright 2023 MONAI Consortium
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
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Monai.Deploy.InformaticsGateway.Test.Plugins;
using Moq;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Common
{
    public class OutputDataPluginEngineFactoryTest
    {
        private readonly Mock<ILogger<OutputDataPluginEngineFactory>> _logger;
        private readonly FileSystem _fileSystem;

        public OutputDataPluginEngineFactoryTest()
        {
            _logger = new Mock<ILogger<OutputDataPluginEngineFactory>>();
            _fileSystem = new FileSystem();

            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        [Fact]
        public void RegisteredPlugins_WhenCalled_ReturnsListOfPlugins()
        {
            var factory = new OutputDataPluginEngineFactory(_fileSystem, _logger.Object);

            var result = factory.RegisteredPlugins();

            Assert.Collection(result,
                p => VerifyPlugin(p, typeof(TestOutputDataPluginAddMessage)),
                p => VerifyPlugin(p, typeof(TestOutputDataPluginModifyDicomFile)));

            _logger.VerifyLogging($"{typeof(IOutputDataPlugin).Name} data plug-in found {typeof(TestOutputDataPluginAddMessage).Name}: {typeof(TestOutputDataPluginAddMessage).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
            _logger.VerifyLogging($"{typeof(IOutputDataPlugin).Name} data plug-in found {typeof(TestOutputDataPluginModifyDicomFile).Name}: {typeof(TestOutputDataPluginModifyDicomFile).GetShortTypeAssemblyName()}.", LogLevel.Information, Times.Once());
        }

        private void VerifyPlugin(KeyValuePair<string, string> values, Type type)
        {
            Assert.Equal(values.Key, type.Name);
            Assert.Equal(values.Value, type.GetShortTypeAssemblyName());
        }
    }
}
