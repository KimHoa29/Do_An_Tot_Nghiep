using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("Save")]
    public class Save
    {
        [Key]
        [Column("save_id")]
        public int SaveId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        // Foreign keys for different types of saved items

        [Column("post_id")]
        public int? PostId { get; set; }
        [Column("document_id")]
        public int? DocumentId { get; set; }
        [Column("topic_id")]
        public int? TopicId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("PostId")]
        public virtual Post Post { get; set; }

        [ForeignKey("DocumentId")]
        public virtual Document Document { get; set; }

        [ForeignKey("TopicId")]
        public virtual Topic Topic { get; set; }
    }
} 