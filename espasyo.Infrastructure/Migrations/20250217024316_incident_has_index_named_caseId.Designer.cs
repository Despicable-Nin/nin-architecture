﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using espasyo.Infrastructure.Data;

#nullable disable

namespace espasyo.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250217024316_incident_has_index_named_caseId")]
    partial class incident_has_index_named_caseId
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("espasyo.Domain.Entities.Incident", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CaseId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("CrimeType")
                        .HasColumnType("int");

                    b.Property<int>("Motive")
                        .HasColumnType("int");

                    b.Property<string>("OtherMotive")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PoliceDistrict")
                        .HasColumnType("int");

                    b.Property<int>("Severity")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("TimeStamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Weather")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CaseId")
                        .IsUnique()
                        .HasFilter("[CaseId] IS NOT NULL");

                    b.ToTable("Incident", (string)null);
                });

            modelBuilder.Entity("espasyo.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });
#pragma warning restore 612, 618
        }
    }
}
