using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("comment")]
    public class Comment
    {
        [Key]
        [Column("comment_id")]
        public int CommentId { get; set; }

        [ForeignKey("Topic")]
        [Column("topic_id")]
        public int TopicId { get; set; }
        public virtual Topic? Topic { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        [Column("content")]
        public string? Content { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
      
        // Thêm liên kết phản hồi cho bình luận
        [ForeignKey("ParentComment")]
        [Column("ParentCommentId")]
        public int? ParentCommentId { get; set; }
        public virtual Comment? ParentComment { get; set; }

        // Thuộc tính mới này sẽ chứa các bình luận con (phản hồi)
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}
