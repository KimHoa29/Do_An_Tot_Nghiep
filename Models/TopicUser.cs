using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("TopicUser")]
    public class TopicUser
    {
        [Column("topic_id")]
        public int TopicId { get; set; }
        public virtual Topic? Topic { get; set; }


        [Column("user_id")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}