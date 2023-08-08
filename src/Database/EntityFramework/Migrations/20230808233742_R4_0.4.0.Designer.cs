﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Monai.Deploy.InformaticsGateway.Database.EntityFramework;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    [DbContext(typeof(InformaticsGatewayContext))]
    [Migration("20230811165855_R4_0.4.0")]
    partial class R4_040
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.21");

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("HostIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Port")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex(new[] { "Name" }, "idx_destination_name")
                        .IsUnique();

                    b.HasIndex(new[] { "Name", "AeTitle", "HostIp", "Port" }, "idx_source_all")
                        .IsUnique();

                    b.ToTable("DestinationApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.DicomAssociationInfo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CalledAeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CallingAeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeDisconnected")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("TEXT");

                    b.Property<string>("Errors")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("FileCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RemoteHost")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("RemotePort")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DicomAssociationHistories");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasColumnOrder(0);

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("AllowedSopClasses")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("Grouping")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IgnoredSopClasses")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PluginAssemblies")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<uint>("Timeout")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("TEXT");

                    b.Property<string>("Workflows")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex(new[] { "Name" }, "idx_monaiae_name")
                        .IsUnique();

                    b.ToTable("MonaiApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest", b =>
                {
                    b.Property<Guid>("InferenceRequestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeCreated")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("InputMetadata")
                        .HasColumnType("TEXT");

                    b.Property<string>("InputResources")
                        .HasColumnType("TEXT");

                    b.Property<string>("OutputResources")
                        .HasColumnType("TEXT");

                    b.Property<byte>("Priority")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TransactionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TryCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("InferenceRequestId");

                    b.HasIndex(new[] { "InferenceRequestId" }, "idx_inferencerequest_inferencerequestid")
                        .IsUnique();

                    b.HasIndex(new[] { "State" }, "idx_inferencerequest_state");

                    b.HasIndex(new[] { "TransactionId" }, "idx_inferencerequest_transactionid")
                        .IsUnique();

                    b.ToTable("InferenceRequests");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CreatedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("HostIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex(new[] { "Name", "AeTitle", "HostIp" }, "idx_source_all")
                        .IsUnique()
                        .HasDatabaseName("idx_source_all1");

                    b.HasIndex(new[] { "Name" }, "idx_source_name")
                        .IsUnique();

                    b.ToTable("SourceApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.Storage.Payload", b =>
                {
                    b.Property<Guid>("PayloadId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CorrelationId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<string>("Files")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MachineName")
                        .HasColumnType("TEXT");

                    b.Property<int>("RetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TaskId")
                        .HasColumnType("TEXT");

                    b.Property<uint>("Timeout")
                        .HasColumnType("INTEGER");

                    b.Property<string>("WorkflowInstanceId")
                        .HasColumnType("TEXT");

                    b.HasKey("PayloadId");

                    b.HasIndex(new[] { "CorrelationId", "PayloadId" }, "idx_payload_ids")
                        .IsUnique();

                    b.HasIndex(new[] { "State" }, "idx_payload_state");

                    b.ToTable("Payloads");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.VirtualApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasColumnOrder(0);

                    b.Property<string>("CreatedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateTimeUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("PluginAssemblies")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UpdatedBy")
                        .HasColumnType("TEXT");

                    b.Property<string>("VirtualAeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Workflows")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.HasIndex(new[] { "Name" }, "idx_virtualae_name")
                        .IsUnique();

                    b.ToTable("VirtualApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Database.Api.StorageMetadataWrapper", b =>
                {
                    b.Property<string>("CorrelationId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Identity")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateTimeCreated")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsUploaded")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TypeName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("CorrelationId", "Identity");

                    b.HasIndex(new[] { "CorrelationId" }, "idx_storagemetadata_correlation");

                    b.HasIndex(new[] { "CorrelationId", "Identity" }, "idx_storagemetadata_ids");

                    b.HasIndex(new[] { "IsUploaded" }, "idx_storagemetadata_uploaded");

                    b.ToTable("StorageMetadataWrapperEntities");
                });
#pragma warning restore 612, 618
        }
    }
}
