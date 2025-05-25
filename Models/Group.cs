using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("group")]
    public class Group
    {
        [Key]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("group_name")]
        public string? GroupName { get; set; }

        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("creator_user_id")]
        public int? CreatorUserId { get; set; }

        // Navigation property đến User (người tạo nhóm)
        [ForeignKey("CreatorUserId")]
        public virtual User? CreatorUser { get; set; }

        public virtual ICollection<GroupUser>? GroupUsers { get; set; }

        public virtual ICollection<TopicGroup>? TopicGroups { get; set; }
        public virtual ICollection<DocumentGroup>? DocumentGroups { get; set; }

        public virtual ICollection<PostGroup>? PostGroups { get; set; }
    }

}
