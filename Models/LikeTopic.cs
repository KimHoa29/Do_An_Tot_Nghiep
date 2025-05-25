using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("LikeTopic")]
    public class LikeTopic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("liketopic_id")]
        public int LikeTopicId { get; set; }

        [Required]
        [Display(Name = "Mã người dùng")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Mã bài viết")]
        [Column("topic_id")]
        public int TopicId { get; set; }

        [Required]
        [Display(Name = "Ngày thích")]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Liên kết rõ ràng với khóa ngoại
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TopicId")]
        public Topic? Topic { get; set; }
    }
}
