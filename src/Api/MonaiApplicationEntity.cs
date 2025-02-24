/*
 * Copyright 2021-2023 MONAI Consortium
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Monai.Deploy.InformaticsGateway.Api.PlugIns;

namespace Monai.Deploy.InformaticsGateway.Api
{
    /// <summary>
    /// MONAI Application Entity
    /// MONAI's SCP AE Title is used to accept incoming associations and can, optionally, map to multiple workflows.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///     "name": "brain-tumor",
    ///     "aeTitle": "BrainTumorModel"
    /// }
    /// </code>
    /// <code>
    /// {
    ///     "name": "COVID-19",
    ///     "aeTitle": "COVID-19",
    ///     "workflows": [ "EXAM", "Delta", "b75cd27a-068a-4f9c-b3da-e5d4ea08c55a"],
    ///     "grouping": [ "0010,0020"],
    ///     "ignoredSopClasses": ["1.2.840.10008.5.1.4.1.1.1.1"],
    ///     "allowedSopClasses": ["1.2.840.10008.5.1.4.1.1.1.2"],
    ///     "timeout": 300
    /// }
    /// </code>
    /// </example>
    public class MonaiApplicationEntity : MongoDBEntityBase
    {
        /// <summary>
        /// Gets or sets the name of a MONAI DICOM application entity.
        /// This value must be unique.
        /// </summary>
        [Key, Column(Order = 0)]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the AE TItle.
        /// </summary>
        public string AeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the DICOM tag used to group the instances.
        /// Defaults to 0020,000D (Study Instance UID).
        /// Valid DICOM Tags: > Study Instance UID (0020,000D) and Series Instance UID (0020,000E).
        /// </summary>
        public string Grouping { get; set; } = default!;

        /// <summary>
        /// Optional field to map AE to one or more workflows.
        /// </summary>
        public List<string> Workflows { get; set; } = default!;

        /// <summary>
        /// Optional list of data input plug-in type names to be executed by the <see cref="IInputDataPlugInEngine"/>.
        /// </summary>
        public List<string> PlugInAssemblies { get; set; } = default!;

        /// <summary>
        /// Optional field to specify SOP Class UIDs to ignore.
        /// <see cref="IgnoredSopClasses"/> and <see cref="AllowedSopClasses"/> are mutually exclusive.
        /// </summary>
        public List<string> IgnoredSopClasses { get; set; } = default!;

        /// <summary>
        /// Optional field to specify accepted SOP Class UIDs.
        /// <see cref="IgnoredSopClasses"/> and <see cref="AllowedSopClasses"/> are mutually exclusive.
        /// </summary>
        public List<string> AllowedSopClasses { get; set; } = default!;

        /// <summary>
        /// Timeout, in seconds, to wait for instances before notifying other subsystems of data arrival
        /// for the specified data group.
        /// Defaults to five seconds.
        /// </summary>
        public uint Timeout { get; set; } = 5;

        /// <summary>
        /// Gets or set the user who created the DICOM entity.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Gets or set the most recent user who updated the DICOM entity.
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Gets or set the most recent date time the DICOM entity was updated.
        /// </summary>
        public DateTime? DateTimeUpdated { get; set; }

        public MonaiApplicationEntity()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = AeTitle;
            }

            if (string.IsNullOrWhiteSpace(Grouping))
            {
                Grouping = "0020,000D";
            }

            Workflows ??= new List<string>();

            IgnoredSopClasses ??= new List<string>();

            AllowedSopClasses ??= new List<string>();

            PlugInAssemblies ??= new List<string>();
        }

        public override string ToString()
        {
            return $"Name: {Name}/AET: {AeTitle}";
        }

        public void SetAuthor(ClaimsPrincipal user, EditMode editMode)
        {
            if (editMode == EditMode.Update)
            {
                DateTimeUpdated = DateTime.UtcNow;
            }

            if (editMode == EditMode.Create)
            {
                CreatedBy = user.Identity?.Name;
            }
            else if (editMode == EditMode.Update)
            {
                UpdatedBy = user.Identity?.Name;
            }
        }
    }
}
