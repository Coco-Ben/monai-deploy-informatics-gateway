﻿/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2020 NVIDIA Corporation
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
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.Common
{
    [Serializable]
    internal class ObjectExistsException : Exception
    {
        public ObjectExistsException()
        {
        }

        public ObjectExistsException(string message) : base(message)
        {
        }

        public ObjectExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ObjectExistsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}