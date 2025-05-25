//using Do_An_Tot_Nghiep.Models;
//using Microsoft.EntityFrameworkCore;
//using System.Threading.Tasks;

//namespace Do_An_Tot_Nghiep.Services
//{
//    public class NotificationService
//    {
//        private readonly AppDbContext _context;

//        public NotificationService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task CreateCommentNotification(Comment comment, string userId)
//        {
//            var topic = await _context.Topics
//                .Include(t => t.User)
//                .FirstOrDefaultAsync(t => t.TopicId == comment.TopicId);

//            if (topic != null && topic.UserId.ToString() != userId)
//            {
//                var notification = new Notification
//                {
//                    UserId = topic.UserId.ToString(),
//                    Title = "Bình luận mới",
//                    Content = $"Có bình luận mới trên bài viết của bạn: {topic.Title}",
//                    Type = "Comment",
//                    RelatedId = comment.CommentId.ToString(),
//                    IsRead = false
//                };

//                _context.Notifications.Add(notification);
//                await _context.SaveChangesAsync();
//            }
//        }

//        public async Task CreateGroupNotification(GroupUser groupUser)
//        {
//            var group = await _context.Groups
//                .FirstOrDefaultAsync(g => g.GroupId == groupUser.GroupId);

//            if (group != null)
//            {
//                var notification = new Notification
//                {
//                    UserId = groupUser.UserId.ToString(),
//                    Title = "Thêm vào nhóm",
//                    Content = $"Bạn đã được thêm vào nhóm: {group.Name}",
//                    Type = "Group",
//                    RelatedId = group.GroupId.ToString(),
//                    IsRead = false
//                };

//                _context.Notifications.Add(notification);
//                await _context.SaveChangesAsync();
//            }
//        }
//    }
//} 