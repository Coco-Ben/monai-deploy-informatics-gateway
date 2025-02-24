/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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
using System.IO;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Monai.Deploy.InformaticsGateway.Configuration
{
    /// <summary>
    /// Validates configuration based on application requirements and DICOM VR requirements.
    /// </summary>
    public class ConfigurationValidator : IValidateOptions<InformaticsGatewayConfiguration>
    {
        private readonly ILogger<ConfigurationValidator> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly List<string> _validationErrors;

        /// <summary>
        /// Initializes an instance of the <see cref="ConfigurationValidator"/> class.
        /// </summary>
        /// <param name="configuration">InformaticsGatewayConfiguration to be validated</param>
        /// <param name="logger">Logger to be used by ConfigurationValidator</param>
        public ConfigurationValidator(ILogger<ConfigurationValidator> logger, IFileSystem fileSystem)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _validationErrors = new List<string>();
        }

        /// <summary>
        /// Checks if InformaticsGatewayConfiguration instance contains valid settings required by the application and conforms to DICOM standards.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public ValidateOptionsResult Validate(string name, InformaticsGatewayConfiguration options)
        {
            var valid = IsDicomScpConfigValid(options.Dicom.Scp);
            valid &= IsDicomScuConfigValid(options.Dicom.Scu);
            valid &= IsDicomWebValid(options.DicomWeb);
            valid &= IsFhirValid(options.Fhir);
            valid &= IsStorageValid(options.Storage);
            valid &= IsHl7Valid(options.Hl7);

#pragma warning disable CA2254 // Template should be a static expression
            _validationErrors.ForEach(p => _logger.Log(LogLevel.Error, p));
#pragma warning restore CA2254 // Template should be a static expression

            return valid ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(string.Join(Environment.NewLine, _validationErrors));
        }

        private bool IsHl7Valid(Hl7Configuration hl7)
        {
            var valid = true;

            valid &= ValidationExtensions.IsPortValid("InformaticsGateway>hl7>port", hl7.Port, _validationErrors);
            return valid;
        }

        private bool IsStorageValid(StorageConfiguration storage)
        {
            var valid = true;
            valid &= IsValidBucketName("InformaticsGateway>storage>bucketName", storage.StorageServiceBucketName);
            valid &= IsValidBucketName("InformaticsGateway>storage>temporaryBucketName", storage.TemporaryStorageBucket);
            valid &= IsNotNullOrWhiteSpace("InformaticsGateway>storage>temporary", storage.RemoteTemporaryStoragePath);
            valid &= IsValueInRange("InformaticsGateway>storage>watermark", 1, 100, storage.Watermark);
            valid &= IsValueInRange("InformaticsGateway>storage>reserveSpaceGB", 1, 999, storage.ReserveSpaceGB);
            valid &= IsValueInRange("InformaticsGateway>storage>payloadProcessThreads", 1, 128, storage.PayloadProcessThreads);
            valid &= IsValueInRange("InformaticsGateway>storage>concurrentUploads", 1, 128, storage.ConcurrentUploads);
            valid &= IsValueInRange("InformaticsGateway>storage>memoryThreshold", 1, int.MaxValue, storage.MemoryThreshold);

            if (storage.TemporaryDataStorage == TemporaryDataStorageLocation.Disk)
            {
                valid &= IsNotNullOrWhiteSpace("InformaticsGateway>storage>localTemporaryStoragePath", storage.LocalTemporaryStoragePath);
                valid &= IsValidDirectory("InformaticsGateway>storage>localTemporaryStoragePath", storage.LocalTemporaryStoragePath);
                valid &= IsValueInRange("InformaticsGateway>storage>bufferSize", 1, int.MaxValue, storage.BufferSize);
            }

            return valid;
        }

        private bool IsValidDirectory(string source, string directory)
        {
            var valid = true;
            try
            {
                if (!_fileSystem.Directory.Exists(directory))
                {
                    _fileSystem.Directory.CreateDirectory(directory);
                }
                using var _ = _fileSystem.File.Create(Path.Combine(directory, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose);
            }
            catch (Exception ex)
            {
                valid = false;
                _validationErrors.Add($"Directory `{directory}` specified in `{source}` is not accessible: {ex.Message}.");
            }
            return valid;
        }

        private bool IsValidBucketName(string source, string bucketName)
        {
            var valid = IsNotNullOrWhiteSpace(source, bucketName);
            var regex = new Regex("(?=^.{3,63}$)(^[a-z0-9]+[a-z0-9\\-]+[a-z0-9]+$)", new RegexOptions(), TimeSpan.FromSeconds(5));
            if (!regex.IsMatch(bucketName))
            {
                valid = false;
                _validationErrors.Add($"Value for {source} does not conform to Amazon S3 bucket naming requirements.");
            }
            return valid;
        }

        private bool IsDicomScpConfigValid(ScpConfiguration scpConfiguration)
        {
            var valid = ValidationExtensions.IsPortValid("InformaticsGateway>dicom>scp>port", scpConfiguration.Port, _validationErrors);
            valid &= IsValueInRange("InformaticsGateway>dicom>scp>max-associations", 1, 1000, scpConfiguration.MaximumNumberOfAssociations);

            return valid;
        }

        private bool IsDicomScuConfigValid(ScuConfiguration scuConfiguration)
        {
            var valid = ValidationExtensions.IsAeTitleValid("InformaticsGateway>dicom>scu>aeTitle.", scuConfiguration.AeTitle, _validationErrors);
            valid &= IsValueInRange("InformaticsGateway>dicom>scu>max-associations", 1, 100, scuConfiguration.MaximumNumberOfAssociations);
            return valid;
        }

        private bool IsDicomWebValid(DicomWebConfiguration configuration)
        {
            var valid = true;

            valid &= IsValueInRange("InformaticsGateway>dicomWeb>clientTimeout.", 1, Int32.MaxValue, configuration.ClientTimeoutSeconds);
            return valid;
        }

        private bool IsFhirValid(FhirConfiguration configuration)
        {
            var valid = true;

            valid &= IsValueInRange("InformaticsGateway>fhir>clientTimeout.", 1, Int32.MaxValue, configuration.ClientTimeoutSeconds);
            return valid;
        }

        private bool IsNotNullOrWhiteSpace(string source, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return true;

            _validationErrors.Add($"Value for {source} is required.");
            return false;
        }

        private bool IsValueInRange(string source, int minValue, int maxValue, int actualValue)
        {
            if (actualValue >= minValue && actualValue <= maxValue) return true;

            _validationErrors.Add($"Value of {source} must be between {minValue} and {maxValue}.");
            return false;
        }

        private bool IsValueInRange(string source, uint minValue, uint maxValue, uint actualValue)
        {
            if (actualValue >= minValue && actualValue <= maxValue) return true;

            _validationErrors.Add($"Value of {source} must be between {minValue} and {maxValue}.");
            return false;
        }
    }
}
