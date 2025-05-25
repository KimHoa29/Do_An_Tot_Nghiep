using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("TopicGroup")]
    public class TopicGroup
    {
        [Column("topic_id")]
        public int TopicId { get; set; }
        public virtual Topic? Topic { get; set; }


        [Column("group_id")]
        public int GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }

}
