﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VUta.Database;

#nullable disable

namespace VUta.Database.Migrations
{
    [DbContext(typeof(VUtaDbContext))]
    [Migration("20230603085220_extendChannelInfo")]
    partial class extendChannelInfo
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("VUta.Database.Models.Channel", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("Banner")
                        .HasColumnType("text")
                        .HasColumnName("banner");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Handle")
                        .HasColumnType("text")
                        .HasColumnName("handle");

                    b.Property<DateTime?>("LastUpdate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_update");

                    b.Property<DateTime?>("LastVideoScan")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_video_scan");

                    b.Property<DateTime?>("NextUpdate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_update");

                    b.Property<Guid?>("NextUpdateId")
                        .HasColumnType("uuid")
                        .HasColumnName("next_update_id");

                    b.Property<DateTime?>("NextVideoScan")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_video_scan");

                    b.Property<long?>("SubscriberCount")
                        .HasColumnType("bigint")
                        .HasColumnName("subscriber_count");

                    b.Property<string>("Thumbnail")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("thumbnail");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.Property<DateTime?>("UnavailableSince")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("unavailable_since");

                    b.Property<long>("VideoCount")
                        .HasColumnType("bigint")
                        .HasColumnName("video_count");

                    b.HasKey("Id")
                        .HasName("pk_channels");

                    b.HasIndex("Handle")
                        .HasDatabaseName("ix_channels_handle");

                    b.HasIndex("LastUpdate")
                        .HasDatabaseName("ix_channels_last_update");

                    b.HasIndex("LastVideoScan")
                        .HasDatabaseName("ix_channels_last_video_scan");

                    b.HasIndex("NextUpdate")
                        .HasDatabaseName("ix_channels_next_update");

                    b.HasIndex("NextUpdateId")
                        .HasDatabaseName("ix_channels_next_update_id");

                    b.HasIndex("NextVideoScan")
                        .HasDatabaseName("ix_channels_next_video_scan");

                    b.ToTable("channels", (string)null);
                });

            modelBuilder.Entity("VUta.Database.Models.Comment", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<DateTime>("LastUpdate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_update");

                    b.Property<long>("LikeCount")
                        .HasColumnType("bigint")
                        .HasColumnName("like_count");

                    b.Property<string>("RepliesId")
                        .HasColumnType("text")
                        .HasColumnName("replies_id");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("text");

                    b.Property<string>("VideoId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("video_id");

                    b.HasKey("Id")
                        .HasName("pk_comments");

                    b.HasIndex("LastUpdate")
                        .HasDatabaseName("ix_comments_last_update");

                    b.HasIndex("VideoId")
                        .HasDatabaseName("ix_comments_video_id");

                    b.ToTable("comments", (string)null);
                });

            modelBuilder.Entity("VUta.Database.Models.Video", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text")
                        .HasColumnName("id");

                    b.Property<string>("ChannelId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("channel_id");

                    b.Property<bool>("IsUta")
                        .HasColumnType("boolean")
                        .HasColumnName("is_uta");

                    b.Property<DateTime?>("LastCommentScan")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_comment_scan");

                    b.Property<DateTime?>("LastUpdate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_update");

                    b.Property<DateTime?>("NextCommentScan")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_comment_scan");

                    b.Property<DateTime?>("NextUpdate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_update");

                    b.Property<Guid?>("NextUpdateId")
                        .HasColumnType("uuid")
                        .HasColumnName("next_update_id");

                    b.Property<DateTime>("PublishDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("publish_date");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.Property<DateTime?>("UnavailableSince")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("unavailable_since");

                    b.HasKey("Id")
                        .HasName("pk_videos");

                    b.HasIndex("ChannelId", "PublishDate")
                        .HasDatabaseName("ix_videos_channel_id_publish_date");

                    b.HasIndex("NextUpdate", "NextUpdateId")
                        .HasDatabaseName("ix_videos_next_update_next_update_id");

                    b.ToTable("videos", (string)null);
                });

            modelBuilder.Entity("VUta.Database.Models.Comment", b =>
                {
                    b.HasOne("VUta.Database.Models.Video", "Video")
                        .WithMany("Comments")
                        .HasForeignKey("VideoId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_comments_videos_video_id");

                    b.Navigation("Video");
                });

            modelBuilder.Entity("VUta.Database.Models.Video", b =>
                {
                    b.HasOne("VUta.Database.Models.Channel", "Channel")
                        .WithMany("Videos")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_videos_channels_channel_id");

                    b.Navigation("Channel");
                });

            modelBuilder.Entity("VUta.Database.Models.Channel", b =>
                {
                    b.Navigation("Videos");
                });

            modelBuilder.Entity("VUta.Database.Models.Video", b =>
                {
                    b.Navigation("Comments");
                });
#pragma warning restore 612, 618
        }
    }
}
