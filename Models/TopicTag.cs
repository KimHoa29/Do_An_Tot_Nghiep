
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("TopicTags")]
    public class TopicTag
    {
        [Column("topic_id")]
        public int TopicId { get; set; }

        [Column("tag_id")]
        public int TagId { get; set; }

        public virtual Topic? Topic { get; set; }
        public virtual Tag? Tag { get; set; }
    }

}
