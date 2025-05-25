using Microsoft.AspNetCore.Mvc.Rendering;

namespace Do_An_Tot_Nghiep.Models
{
    public class TopicViewModel
    {
        public Topic Topic { get; set; } = new Topic();

        public List<int>? SelectedGroupIds { get; set; }  // Cho visibility_type = group
        public List<int>? SelectedUserIds { get; set; }   // Cho visibility_type = custom

        public MultiSelectList? AllGroups { get; set; }
        public MultiSelectList? AllUsers { get; set; }

        public int[] SelectedTagIds { get; set; } = [];
    }

}
