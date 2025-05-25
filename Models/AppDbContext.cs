using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<LecturerStudent> LecturerStudents { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<TopicTag> TopicTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupUser> GroupUsers { get; set; }
        public DbSet<TopicGroup> TopicGroups { get; set; }
        public DbSet<TopicUser> TopicUsers { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentGroup> DocumentGroups { get; set; }
        public DbSet<DocumentUser> DocumentUsers { get; set; }
        public DbSet<CommentDocument> CommentDocuments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostGroup> PostGroups { get; set; }
        public DbSet<PostUser> PostUsers { get; set; }
        public DbSet<CommentPost> CommentPosts { get; set; }
        public DbSet<LikeDocument> LikeDocuments { get; set; }
        public DbSet<LikeTopic> LikeTopics { get; set; }
        public DbSet<LikePost> LikePosts { get; set; }
        public DbSet<PostMention> PostMentions { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Lecturer>().ToTable("Lecturer");
            modelBuilder.Entity<Student>().ToTable("Student");
            modelBuilder.Entity<Topic>().ToTable("Topic");
            modelBuilder.Entity<Tag>().ToTable("Tag");
            modelBuilder.Entity<Comment>().ToTable("Comment");
            modelBuilder.Entity<Group>().ToTable("Group");
            modelBuilder.Entity<Document>().ToTable("Document");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<CommentDocument>().ToTable("CommentDocument");
            modelBuilder.Entity<Post>().ToTable("Post");
            modelBuilder.Entity<CommentPost>().ToTable("CommentPost");

            modelBuilder.Entity<Notification>()
               .HasOne(n => n.User)
               .WithMany()
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);
            // Khóa chính cho bảng LecturerStudent
            modelBuilder.Entity<LecturerStudent>()
                .HasKey(ls => new { ls.LecturerId, ls.StudentId });

            // Cấu hình DeleteBehavior.Cascade khi xóa giảng viên
            modelBuilder.Entity<Lecturer>()
                .HasMany(l => l.LecturerStudents)
                .WithOne(ls => ls.Lecturer)
                .HasForeignKey(ls => ls.LecturerId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa các bản ghi liên quan trong LecturerStudents khi xóa giảng viên

            // Cấu hình DeleteBehavior.Cascade khi xóa sinh viên
            modelBuilder.Entity<Student>()
                .HasMany(s => s.LecturerStudents)
                .WithOne(ls => ls.Student)
                .HasForeignKey(ls => ls.StudentId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa các bản ghi liên quan trong LecturerStudents khi xóa sinh viên

            // Cấu hình bảng Topic
            modelBuilder.Entity<Topic>()
                .HasOne(t => t.User)
                .WithMany() // hoặc .WithMany(u => u.Topics) nếu có navigation trong User
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa cascade khi xóa người dùng

            // Cấu hình nhiều-nhiều giữa Topic và Tag qua TopicTag
            modelBuilder.Entity<TopicTag>()
                .HasKey(tt => new { tt.TopicId, tt.TagId });

            modelBuilder.Entity<TopicTag>()
                .HasOne(tt => tt.Topic)
                .WithMany(t => t.TopicTags)
                .HasForeignKey(tt => tt.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopicTag>()
                .HasOne(tt => tt.Tag)
                .WithMany(t => t.TopicTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ giữa Topic và Comment
            modelBuilder.Entity<Topic>()
                .HasMany(t => t.Comments)  // Một Topic có thể có nhiều Comment
                .WithOne(c => c.Topic)  // Mỗi Comment thuộc về một Topic
                .HasForeignKey(c => c.TopicId)  // Khóa ngoại TopicId trong Comment
                .OnDelete(DeleteBehavior.Cascade);  // Xóa cascade khi xóa Topic

            // Cấu hình mối quan hệ giữa User và Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)  // Mỗi Comment thuộc về một User
                .WithMany()  // Một User có thể có nhiều Comment
                .HasForeignKey(c => c.UserId)  // Khóa ngoại UserId trong Comment
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình khóa chính cho GroupUser 
            modelBuilder.Entity<GroupUser>()
                .HasKey(gu => new { gu.GroupId, gu.UserId }); // Hoặc dùng (gu => new { gu.GroupId, gu.UserId }) nếu dùng composite key

            // Cấu hình quan hệ Group - GroupUser
            modelBuilder.Entity<Group>()
                .HasMany(g => g.GroupUsers)
                .WithOne(gu => gu.Group)
                .HasForeignKey(gu => gu.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa Group sẽ xóa GroupUser

            // Cấu hình quan hệ User - GroupUser
            modelBuilder.Entity<User>()
                .HasMany(u => u.GroupUsers)
                .WithOne(gu => gu.User)
                .HasForeignKey(gu => gu.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa User sẽ xóa GroupUser

            modelBuilder.Entity<TopicGroup>()
                .HasKey(tg => new { tg.TopicId, tg.GroupId });

            modelBuilder.Entity<TopicGroup>()
                .HasOne(tg => tg.Topic)
                .WithMany(t => t.TopicGroups)
                .HasForeignKey(tg => tg.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopicGroup>()
                .HasOne(tg => tg.Group)
                .WithMany(g => g.TopicGroups)
                .HasForeignKey(tg => tg.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopicUser>()
                .HasKey(tu => new { tu.TopicId, tu.UserId });

            modelBuilder.Entity<TopicUser>()
                .HasOne(tu => tu.Topic)
                .WithMany(t => t.TopicUsers)
                .HasForeignKey(tu => tu.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TopicUser>()
                .HasOne(tu => tu.User)
                .WithMany(u => u.TopicUsers)
                .HasForeignKey(tu => tu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ phản hồi giữa các Comment
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment) // Một comment có thể là phản hồi của comment khác
                .WithMany(c => c.Replies)     // Một comment có nhiều phản hồi
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa phản hồi nếu comment cha bị xóa

            // Cấu hình bảng Document
            modelBuilder.Entity<Document>()
                .HasOne(t => t.User)
                .WithMany() // hoặc .WithMany(u => u.Topics) nếu có navigation trong User
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa cascade khi xóa người dùng

            modelBuilder.Entity<DocumentGroup>()
     .HasKey(tg => new { tg.DocumentId, tg.GroupId });

            modelBuilder.Entity<DocumentGroup>()
                .HasOne(tg => tg.Document)
                .WithMany(t => t.DocumentGroups)
                .HasForeignKey(tg => tg.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentGroup>()
                .HasOne(tg => tg.Group)
                .WithMany(g => g.DocumentGroups)
                .HasForeignKey(tg => tg.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DocumentUser>()
                .HasKey(tu => new { tu.DocumentId, tu.UserId });

            modelBuilder.Entity<DocumentUser>()
                .HasOne(tu => tu.Document)
                .WithMany(t => t.DocumentUsers)
                .HasForeignKey(tu => tu.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Cấu hình mối quan hệ giữa User và CommentDocument
            modelBuilder.Entity<CommentDocument>()
                .HasOne(c => c.User)  // Mỗi CommentDocument thuộc về một User
                .WithMany()  // Một User có thể có nhiều CommentDocument
                .HasForeignKey(c => c.UserId)  // Khóa ngoại UserId trong CommentDocument
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ phản hồi giữa các CommentDocument
            modelBuilder.Entity<CommentDocument>()
                .HasOne(c => c.ParentComment) // Một comment có thể là phản hồi của comment khác
                .WithMany(c => c.Replies)     // Một comment có nhiều phản hồi
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình bảng Post
            modelBuilder.Entity<Post>()
                .HasOne(t => t.User)
                .WithMany() 
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa cascade khi xóa người dùng

            modelBuilder.Entity<PostGroup>()
     .HasKey(tg => new { tg.PostId, tg.GroupId });

            modelBuilder.Entity<PostGroup>()
                .HasOne(tg => tg.Post)
                .WithMany(t => t.PostGroups)
                .HasForeignKey(tg => tg.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostGroup>()
                .HasOne(tg => tg.Group)
                .WithMany(g => g.PostGroups)
                .HasForeignKey(tg => tg.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostUser>()
                .HasKey(tu => new { tu.PostId, tu.UserId });

            modelBuilder.Entity<PostUser>()
                .HasOne(tu => tu.Post)
                .WithMany(t => t.PostUsers)
                .HasForeignKey(tu => tu.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ giữa User và CommentDocument
            modelBuilder.Entity<CommentPost>()
                .HasOne(c => c.User)  // Mỗi CommentDocument thuộc về một User
                .WithMany()  // Một User có thể có nhiều CommentDocument
                .HasForeignKey(c => c.UserId)  // Khóa ngoại UserId trong CommentDocument
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ phản hồi giữa các CommentDocument
            modelBuilder.Entity<CommentPost>()
                .HasOne(c => c.ParentComment) // Một comment có thể là phản hồi của comment khác
                .WithMany(c => c.Replies)     // Một comment có nhiều phản hồi
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LikePost>()
                .HasIndex(lp => new { lp.UserId, lp.PostId })
                .IsUnique(); // ngăn like trùng

            modelBuilder.Entity<LikePost>()
                .HasOne(l => l.User)
                .WithMany(u => u.LikePosts)
                .HasForeignKey(l => l.UserId);

            modelBuilder.Entity<LikePost>()
                .HasOne(l => l.Post)
                .WithMany(p => p.LikePosts)
                .HasForeignKey(l => l.PostId);

            modelBuilder.Entity<LikeTopic>()
                .HasIndex(lp => new { lp.UserId, lp.TopicId })
                .IsUnique(); // ngăn like trùng

            modelBuilder.Entity<LikeTopic>()
                .HasOne(l => l.User)
                .WithMany(u => u.LikeTopics)
                .HasForeignKey(l => l.UserId);

            modelBuilder.Entity<LikeTopic>()
                .HasOne(l => l.Topic)
                .WithMany(p => p.LikeTopics)
                .HasForeignKey(l => l.TopicId);

            modelBuilder.Entity<LikeDocument>()
                .HasIndex(lp => new { lp.UserId, lp.DocumentId })
                .IsUnique(); // ngăn like trùng

            modelBuilder.Entity<LikeDocument>()
                .HasOne(l => l.User)
                .WithMany(u => u.LikeDocuments)
                .HasForeignKey(l => l.UserId);

            modelBuilder.Entity<LikeDocument>()
                .HasOne(l => l.Document)
                .WithMany(p => p.LikeDocuments)
                .HasForeignKey(l => l.DocumentId);

            // Configure PostMention relationships
            modelBuilder.Entity<PostMention>()
                .HasOne(pm => pm.Post)
                .WithMany(p => p.PostMentions)
                .HasForeignKey(pm => pm.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostMention>()
                .HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
