﻿/*
 * Copyright 2022 MONAI Consortium
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
    internal class FileMoveException : Exception
    {
        public FileMoveException()
        {
        }

        public FileMoveException(string message) : base(message)
        {
        }

        public FileMoveException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public FileMoveException(string source, string destination, Exception ex)
            : this($"Exception moving file from {source} to {destination}.", ex)
        {
        }

        protected FileMoveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
