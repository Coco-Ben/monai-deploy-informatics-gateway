/*
 * Copyright 2021-2022 MONAI Consortium
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

using System.IO;
using Ardalis.GuardClauses;

namespace Monai.Deploy.InformaticsGateway.CLI.Services
{
    public interface IEmbeddedResource
    {
        Stream GetManifestResourceStream(string name);
    }

    public class EmbeddedResource : IEmbeddedResource
    {
        public Stream GetManifestResourceStream(string name)
        {
            Guard.Against.NullOrWhiteSpace(name, nameof(name));

            return GetType().Assembly.GetManifestResourceStream(name);
        }
    }
}
