﻿// Copyright 2022 MONAI Consortium
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Configuration;
using System;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class DynamicServiceLocator
    {
        internal static TInterface LocateService<TInterface>(IServiceProvider serviceProvider, ILogger<Program> logger, string fullyQualifiedTypeString)
        {
            var type = Type.GetType(fullyQualifiedTypeString);
            if (type is null)
            {
                throw new ConfigurationException($"Type '{fullyQualifiedTypeString}' cannot be found.");
            }

            var instance = serviceProvider.GetService(type);
            if (instance is null)
            {
                throw new ConfigurationException($"Instance '{fullyQualifiedTypeString}' cannot be created.");
            }

            return (TInterface)instance;
        }
    }
}
