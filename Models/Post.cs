using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("post")]
    public class Post
    {
        [Key]
        [Column("post_id")]
        [DisplayName("Mã tài liệu")]
        public int PostId { get; set; }

        [Required]
        [Column("title")]
        [DisplayName("Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Column("content")]
        [DisplayName("Nội dung")]
        public string? Content { get; set; }  // nullable để tránh lỗi nếu null từ DB

        [Required]
        [Column("visibility_type")]
        [DisplayName("Loại hiển thị")]
        public string VisibilityType { get; set; } = "public";

        [Column("created_at")]
        [DisplayName("Ngày tạo")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        [DisplayName("Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [Column("image_url")]
        [DisplayName("Ảnh")]
        public string? ImageUrl { get; set; }

        [Column("file_url")]
        [DisplayName("Tệp đính kèm")]
        public string? FileUrl { get; set; }

        [Required]
        [Column("user_id")]
        [DisplayName("Người dùng")]
        public int UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public virtual ICollection<PostGroup>? PostGroups { get; set; }

        public virtual ICollection<PostUser>? PostUsers { get; set; }

        public virtual ICollection<CommentPost>? CommentPosts { get; set; }

        public virtual ICollection<LikePost>? LikePosts { get; set; }

        public virtual ICollection<PostMention>? PostMentions { get; set; }

        public virtual ICollection<Save>? Saves { get; set; }
    }
}
