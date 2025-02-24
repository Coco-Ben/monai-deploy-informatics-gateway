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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Monai.Deploy.InformaticsGateway.Api;

namespace Monai.Deploy.InformaticsGateway.Database.EntityFramework.Configuration
{
    internal class DestinationApplicationEntityConfiguration : IEntityTypeConfiguration<DestinationApplicationEntity>
    {
        public void Configure(EntityTypeBuilder<DestinationApplicationEntity> builder)
        {
            builder.HasKey(j => j.Name);
            builder.Property(j => j.AeTitle).IsRequired();
            builder.Property(j => j.Port).IsRequired();
            builder.Property(j => j.HostIp).IsRequired();
            builder.Property(j => j.CreatedBy).IsRequired(false);
            builder.Property(j => j.UpdatedBy).IsRequired(false);
            builder.Property(j => j.DateTimeCreated).IsRequired();
            builder.Property(j => j.DateTimeUpdated).IsRequired(false);

            builder.HasIndex(p => p.Name, "idx_destination_name").IsUnique();
            builder.HasIndex(p => new { p.Name, p.AeTitle, p.HostIp, p.Port }, "idx_source_all").IsUnique();

            builder.Ignore(p => p.Id);
        }
    }
}
