﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Monai.Deploy.InformaticsGateway.Database;

#nullable disable

namespace Monai.Deploy.InformaticsGateway.Database.Migrations
{
    [DbContext(typeof(InformaticsGatewayContext))]
    partial class InformaticsGatewayContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.1");

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.DestinationApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("HostIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Port")
                        .HasColumnType("INTEGER");

                    b.HasKey("Name");

                    b.ToTable("DestinationApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.MonaiApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT")
                        .HasColumnOrder(0);

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Grouping")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IgnoredSopClasses")
                        .HasColumnType("TEXT");

                    b.Property<uint>("Timeout")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Workflows")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.ToTable("MonaiApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.Rest.InferenceRequest", b =>
                {
                    b.Property<Guid>("InferenceRequestId")
                        .ValueGeneratedOnAdd()
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

                    b.Property<string>("StoragePath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TransactionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TryCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("InferenceRequestId");

                    b.ToTable("InferenceRequest");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.SourceApplicationEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("AeTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("HostIp")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.ToTable("SourceApplicationEntities");
                });

            modelBuilder.Entity("Monai.Deploy.InformaticsGateway.Api.Storage.Payload", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Files")
                        .HasColumnType("TEXT");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("RetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("Timeout")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Payload");
                });
#pragma warning restore 612, 618
        }
    }
}
