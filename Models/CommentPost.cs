using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("comment_post")]
    public class CommentPost
    {
        [Key]
        [Column("comment_post_id")]
        public int CommentPostId { get; set; }

        [ForeignKey("Post")]
        [Column("post_id")]
        public int PostId { get; set; }
        public virtual Post? Post { get; set; }

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

        [ForeignKey("ParentComment")]
        public int? ParentCommentId { get; set; }
        public virtual CommentPost? ParentComment { get; set; }

        public virtual ICollection<CommentPost> Replies { get; set; } = new List<CommentPost>();
    }
}