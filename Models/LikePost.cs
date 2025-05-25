using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("LikePost")]
    public class LikePost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("likepost_id")]
        public int LikePostId { get; set; }

        [Required]
        [Display(Name = "Mã người dùng")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Mã bài viết")]
        [Column("post_id")]
        public int PostId { get; set; }

        [Required]
        [Display(Name = "Ngày thích")]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Liên kết rõ ràng với khóa ngoại
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("PostId")]
        public Post? Post { get; set; }
    }
}
