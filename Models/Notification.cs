using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int Type { get; set; } // "1" là comment, "2" là group, "3" là topic


        public int? GroupId { get; set; }

        public int? TopicId { get; set; }
        public int? CommentId { get; set; }

        public int? DocumentId { get; set; }
        public int? CommentDocumentId { get; set; }

        public int? PostId { get; set; }
        public int? CommentPostId { get; set; }
        public bool IsRead { get; set; } = false;
        public string? Path { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }

        [ForeignKey("TopicId")]
        public virtual Topic? Topic { get; set; }

        [ForeignKey("CommentId")]
        public virtual Comment? Comment { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document? Document { get; set; }

        [ForeignKey("CommentDocumentId")]
        public virtual CommentDocument? CommentDocument { get; set; }

        [ForeignKey("PostId")]
        public virtual Post? Post { get; set; }

        [ForeignKey("CommentPostId")]
        public virtual CommentDocument? CommentPost { get; set; }
    }
} 