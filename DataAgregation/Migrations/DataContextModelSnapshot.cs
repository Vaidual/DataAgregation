﻿// <auto-generated />
using System;
using DataAgregation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace DataAgregation.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("DataAgregation.Models.CurrencyPurchase", b =>
                {
                    b.Property<int>("CurrencyPurchaseId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CurrencyPurchaseId"));

                    b.Property<int>("EventId")
                        .HasColumnType("int");

                    b.Property<int>("Income")
                        .HasColumnType("int");

                    b.Property<string>("PackName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("CurrencyPurchaseId");

                    b.HasIndex("EventId");

                    b.ToTable("CurrencyPurchases");
                });

            modelBuilder.Entity("DataAgregation.Models.Event", b =>
                {
                    b.Property<int>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EventId"));

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("EventType")
                        .HasColumnType("int");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("EventId");

                    b.HasIndex("UserId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("DataAgregation.Models.IngamePurchase", b =>
                {
                    b.Property<int>("IngamePurchaseId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("IngamePurchaseId"));

                    b.Property<int>("EventId")
                        .HasColumnType("int");

                    b.Property<string>("ItemName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Price")
                        .HasColumnType("int");

                    b.HasKey("IngamePurchaseId");

                    b.HasIndex("EventId");

                    b.ToTable("IngamePurchases");
                });

            modelBuilder.Entity("DataAgregation.Models.StageEnd", b =>
                {
                    b.Property<int>("StageEndId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("StageEndId"));

                    b.Property<int>("EventId")
                        .HasColumnType("int");

                    b.Property<int?>("Income")
                        .HasColumnType("int");

                    b.Property<bool>("IsWon")
                        .HasColumnType("bit");

                    b.Property<int>("Stage")
                        .HasColumnType("int");

                    b.Property<int>("Time")
                        .HasColumnType("int");

                    b.HasKey("StageEndId");

                    b.HasIndex("EventId");

                    b.ToTable("StageEnds");
                });

            modelBuilder.Entity("DataAgregation.Models.StageStart", b =>
                {
                    b.Property<int>("StageStartId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("StageStartId"));

                    b.Property<int>("EventId")
                        .HasColumnType("int");

                    b.Property<int>("Stage")
                        .HasColumnType("int");

                    b.HasKey("StageStartId");

                    b.HasIndex("EventId");

                    b.ToTable("StageStarts");
                });

            modelBuilder.Entity("DataAgregation.Models.User", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Age")
                        .HasColumnType("int");

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("DataAgregation.Models.CurrencyPurchase", b =>
                {
                    b.HasOne("DataAgregation.Models.Event", "Event")
                        .WithMany("CurrencyPurchases")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("DataAgregation.Models.Event", b =>
                {
                    b.HasOne("DataAgregation.Models.User", "User")
                        .WithMany("Events")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("DataAgregation.Models.IngamePurchase", b =>
                {
                    b.HasOne("DataAgregation.Models.Event", "Event")
                        .WithMany("IngamePurchases")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("DataAgregation.Models.StageEnd", b =>
                {
                    b.HasOne("DataAgregation.Models.Event", "Event")
                        .WithMany("StageEnds")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("DataAgregation.Models.StageStart", b =>
                {
                    b.HasOne("DataAgregation.Models.Event", "Event")
                        .WithMany("StageStarts")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("DataAgregation.Models.Event", b =>
                {
                    b.Navigation("CurrencyPurchases");

                    b.Navigation("IngamePurchases");

                    b.Navigation("StageEnds");

                    b.Navigation("StageStarts");
                });

            modelBuilder.Entity("DataAgregation.Models.User", b =>
                {
                    b.Navigation("Events");
                });
#pragma warning restore 612, 618
        }
    }
}
