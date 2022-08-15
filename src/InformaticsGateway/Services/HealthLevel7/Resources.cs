/*
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

namespace Monai.Deploy.InformaticsGateway.Services.HealthLevel7
{
    internal static class Resources
    {
        public const int AcceptAcknowledgementType = 15;

        public const char AsciiVT = (char)0x0B;
        public const char AsciiFS = (char)0x1C;

        public const string AcknowledgmentTypeNever = "NE";
        public const string AcknowledgmentTypeError = "ER";
        public const string AcknowledgmentTypeSuccessful = "SU";

        public const string MessageHeaderSegment = "MSH";
    }
}
