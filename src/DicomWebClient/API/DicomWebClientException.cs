/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.Net;
using System.Runtime.Serialization;

namespace Monai.Deploy.InformaticsGateway.DicomWeb.Client.API
{
    [Serializable]
    public class DicomWebClientException : Exception
    {
        public HttpStatusCode? StatusCode { get; }
        public string ResponseMessage { get; }

        public DicomWebClientException(HttpStatusCode? statusCode, string responseMessage, Exception innerException)
            : base(responseMessage, innerException)
        {
            StatusCode = statusCode;
            ResponseMessage = responseMessage;
        }

        protected DicomWebClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            StatusCode = (HttpStatusCode?)info.GetValue(nameof(StatusCode), typeof(HttpStatusCode?));
            ResponseMessage = info.GetString(nameof(ResponseMessage));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(StatusCode), StatusCode, typeof(HttpStatusCode?));
            info.AddValue(nameof(ResponseMessage), ResponseMessage);

            base.GetObjectData(info, context);
        }
    }
}
