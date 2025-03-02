/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2021 NVIDIA Corporation
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

using Monai.Deploy.InformaticsGateway.Api;
using MongoDB.Bson.Serialization;

namespace Monai.Deploy.InformaticsGateway.Database.MongoDB.Configurations
{
    internal static class MonaiApplicationEntityConfiguration
    {
        public static void Configure()
        {
            BsonClassMap.RegisterClassMap<MonaiApplicationEntity>(j =>
            {
                j.AutoMap();
                j.SetIgnoreExtraElements(true);
            });
        }
    }
}
