using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace Prevueit.Db.Models
{
    public partial class prevuitContext : DbContext
    {

        public prevuitContext(DbContextOptions<prevuitContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Admin> Admin { get; set; }
        public virtual DbSet<Comment> Comment { get; set; }
        public virtual DbSet<FileStorage> FileStorage { get; set; }
        public virtual DbSet<FileToSharedUser> FileToSharedUser { get; set; }
        public virtual DbSet<Folder> Folder { get; set; }
        public virtual DbSet<FolderToSharedUser> FolderToSharedUser { get; set; }
        public virtual DbSet<Notification> Notification { get; set; }
        public virtual DbSet<SpaceConfiguration> SpaceConfiguration { get; set; }
        public virtual DbSet<UserInfo> UserInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=SG2NWPLS19SQL-v07.mssql.shr.prod.sin2.secureserver.net;Database=prevuit;User ID=prevuit;PersistSecurityInfo=False;Password=Arvaan@123;Encrypt=true;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:DefaultSchema", "prevuit");

            modelBuilder.Entity<Admin>(entity =>
            {
                entity.HasKey(e => e.IAdminId);

                entity.ToTable("Admin", "dbo");

                entity.Property(e => e.IAdminId).HasColumnName("iAdminId");

                entity.Property(e => e.LastLoginDate)
                    .HasColumnName("lastLoginDate")
                    .HasColumnType("datetime");

                entity.Property(e => e.MobileNumber).HasMaxLength(50);

                entity.Property(e => e.Otp).HasColumnName("OTP");

                entity.Property(e => e.UserName).HasMaxLength(200);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.ICommentId);

                entity.ToTable("Comment", "dbo");

                entity.Property(e => e.ICommentId).HasColumnName("iCommentId");

                entity.Property(e => e.CommentCreateDate).HasColumnType("datetime");

                entity.Property(e => e.CommentType).HasMaxLength(50);

                entity.Property(e => e.CommentUpdateDate).HasColumnType("datetime");

                entity.Property(e => e.CommentUrl)
                    .HasColumnName("CommentURL")
                    .HasMaxLength(100);

                entity.Property(e => e.IFileId).HasColumnName("iFileId");

                entity.Property(e => e.IParentCommentId).HasColumnName("iParentCommentId");

                entity.Property(e => e.IUserId).HasColumnName("iUserId");

                entity.Property(e => e.VideoFrametime).HasMaxLength(50);
            });

            modelBuilder.Entity<FileStorage>(entity =>
            {
                entity.HasKey(e => e.IFileId);

                entity.ToTable("FileStorage", "dbo");

                entity.Property(e => e.IFileId).HasColumnName("iFileId");

                entity.Property(e => e.AzureFileName).HasMaxLength(500);

                entity.Property(e => e.ContentType).HasMaxLength(50);

                entity.Property(e => e.DuplicateByEmail).HasMaxLength(100);

                entity.Property(e => e.DuplicateDate).HasColumnType("datetime");

                entity.Property(e => e.ExpiryDate).HasColumnType("datetime");

                entity.Property(e => e.FileOriginalName).HasMaxLength(100);

                entity.Property(e => e.FileSize).HasMaxLength(50);

                entity.Property(e => e.FileType).HasMaxLength(50);

                entity.Property(e => e.FileUrl).HasColumnName("FileURL");

                entity.Property(e => e.IFolderId)
                    .HasColumnName("iFolderId")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.IUserInfoId).HasColumnName("iUserInfoId");

                entity.Property(e => e.IsFavourite)
                    .HasColumnName("isFavourite")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.ReviewedByEmail).HasMaxLength(100);

                entity.Property(e => e.ReviewedDate).HasColumnType("datetime");

                entity.Property(e => e.SharebleLink).HasMaxLength(250);

                entity.Property(e => e.UploadedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<FileToSharedUser>(entity =>
            {
                entity.HasKey(e => e.IFileToSharedUserId);

                entity.ToTable("FileToSharedUser", "dbo");

                entity.Property(e => e.IFileToSharedUserId).HasColumnName("iFileToSharedUserId");

                entity.Property(e => e.FromUserEmail).HasMaxLength(100);

                entity.Property(e => e.IFileId).HasColumnName("iFileId");

                entity.Property(e => e.UploadedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Folder>(entity =>
            {
                entity.HasKey(e => e.IFolderId);

                entity.ToTable("Folder", "dbo");

                entity.Property(e => e.IFolderId).HasColumnName("iFolderId");

                entity.Property(e => e.FolderCreateDate).HasColumnType("datetime");

                entity.Property(e => e.FolderName).HasMaxLength(200);

                entity.Property(e => e.IUserId).HasColumnName("iUserId");

                entity.Property(e => e.IsFavourite).HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<FolderToSharedUser>(entity =>
            {
                entity.HasKey(e => e.IFolderToSharedUserId);

                entity.ToTable("FolderToSharedUser", "dbo");

                entity.Property(e => e.IFolderToSharedUserId).HasColumnName("iFolderToSharedUserId");

                entity.Property(e => e.FromUserEmail).HasMaxLength(100);

                entity.Property(e => e.IFolderId).HasColumnName("iFolderId");

                entity.Property(e => e.UploadedDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.INotificationId);

                entity.ToTable("Notification", "dbo");

                entity.Property(e => e.INotificationId).HasColumnName("iNotificationId");

                entity.Property(e => e.CreateDate).HasColumnType("datetime");

                entity.Property(e => e.FromUserName).HasMaxLength(200);

                entity.Property(e => e.IUserId).HasColumnName("iUserId");

                entity.Property(e => e.IsActive).HasDefaultValueSql("((1))");

                entity.Property(e => e.Title).HasMaxLength(400);
            });

            modelBuilder.Entity<SpaceConfiguration>(entity =>
            {
                entity.HasKey(e => e.ISpaceConfigurationId);

                entity.ToTable("SpaceConfiguration", "dbo");

                entity.Property(e => e.ISpaceConfigurationId).HasColumnName("iSpaceConfigurationId");

                entity.Property(e => e.ExpiryDate).HasColumnType("datetime");

                entity.Property(e => e.PerFileUploadLimit).HasMaxLength(50);

                entity.Property(e => e.TotalSpaceAllowed).HasMaxLength(50);

                entity.Property(e => e.UserType).HasMaxLength(50);
            });

            modelBuilder.Entity<UserInfo>(entity =>
            {
                entity.HasKey(e => e.IUserInfoId);

                entity.ToTable("UserInfo", "dbo");

                entity.Property(e => e.IUserInfoId).HasColumnName("iUserInfoId");

                entity.Property(e => e.BlockDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedDate).HasColumnType("datetime");

                entity.Property(e => e.ExpiryDate).HasColumnType("datetime");

                entity.Property(e => e.FirstName).HasMaxLength(50);

                entity.Property(e => e.IsPaidUser).HasDefaultValueSql("((0))");

                entity.Property(e => e.IsPermanentyBlock)
                    .HasColumnName("isPermanentyBlock")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.IsProfileChanged)
                    .HasColumnName("isProfileChanged")
                    .HasDefaultValueSql("((0))");

                entity.Property(e => e.LastName).HasMaxLength(50);

                entity.Property(e => e.ProfilePicUrl)
                    .HasColumnName("profilePicURL")
                    .HasMaxLength(200);

                entity.Property(e => e.Remarks).HasMaxLength(200);

                entity.Property(e => e.TempName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.UserEmail)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
