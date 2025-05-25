using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class StatisticsController : BaseController
    {
        private readonly AppDbContext _context;

        public StatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            // User statistics
            var userStats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                Lecturers = await _context.Users.CountAsync(u => u.Role == "Lecturer"),
                Students = await _context.Users.CountAsync(u => u.Role == "Student"),
                Admins = await _context.Users.CountAsync(u => u.Role == "Admin")
            };

            // Content statistics
            var contentStats = new
            {
                TotalDocuments = await _context.Documents.CountAsync(),
                PublicDocuments = await _context.Documents.CountAsync(d => d.VisibilityType == "public"),
                PrivateDocuments = await _context.Documents.CountAsync(d => d.VisibilityType == "private"),
                GroupDocuments = await _context.Documents.CountAsync(d => d.VisibilityType == "group"),
                CustomDocuments = await _context.Documents.CountAsync(d => d.VisibilityType == "custom"),

                TotalTopics = await _context.Topics.CountAsync(),
                PublicTopics = await _context.Topics.CountAsync(t => t.VisibilityType == "public"),
                PrivateTopics = await _context.Topics.CountAsync(t => t.VisibilityType == "private"),
                GroupTopics = await _context.Topics.CountAsync(t => t.VisibilityType == "group"),
                CustomTopics = await _context.Topics.CountAsync(t => t.VisibilityType == "custom"),

                TotalPosts = await _context.Posts.CountAsync(),
                PublicPosts = await _context.Posts.CountAsync(p => p.VisibilityType == "public"),
                PrivatePosts = await _context.Posts.CountAsync(p => p.VisibilityType == "private"),
                GroupPosts = await _context.Posts.CountAsync(p => p.VisibilityType == "group"),
                CustomPosts = await _context.Posts.CountAsync(p => p.VisibilityType == "custom")
            };

            // Group statistics
            var groupStats = new
            {
                TotalGroups = await _context.Groups.CountAsync(),
                TotalGroupUsers = await _context.GroupUsers.CountAsync(),
                AverageUsersPerGroup = await _context.Groups
                    .Select(g => g.GroupUsers.Count)
                    .DefaultIfEmpty()
                    .AverageAsync()
            };

            // Comment statistics
            var commentStats = new
            {
                TotalComments = await _context.Comments.CountAsync() +
                               await _context.CommentDocuments.CountAsync() +
                               await _context.CommentPosts.CountAsync(),
                DocumentComments = await _context.CommentDocuments.CountAsync(),
                TopicComments = await _context.Comments.CountAsync(),
                PostComments = await _context.CommentPosts.CountAsync()
            };

            // Activity over time (last 30 days)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var activityStats = new
            {
                NewDocuments = await _context.Documents
                    .CountAsync(d => d.CreatedAt >= thirtyDaysAgo),
                NewTopics = await _context.Topics
                    .CountAsync(t => t.CreatedAt >= thirtyDaysAgo),
                NewPosts = await _context.Posts
                    .CountAsync(p => p.CreatedAt >= thirtyDaysAgo),
                NewComments = await _context.Comments
                    .CountAsync(c => c.CreatedAt >= thirtyDaysAgo) +
                    await _context.CommentDocuments
                    .CountAsync(c => c.CreatedAt >= thirtyDaysAgo) +
                    await _context.CommentPosts
                    .CountAsync(c => c.CreatedAt >= thirtyDaysAgo)
            };

            // Most active users
            var mostActiveUsers = await _context.Users
                .Select(u => new
                {
                    UserName = u.Username,
                    Role = u.Role,
                    DocumentCount = _context.Documents.Count(d => d.UserId == u.UserId),
                    TopicCount = _context.Topics.Count(t => t.UserId == u.UserId),
                    PostCount = _context.Posts.Count(p => p.UserId == u.UserId),
                    CommentCount = _context.Comments.Count(c => c.UserId == u.UserId) +
                                 _context.CommentDocuments.Count(c => c.UserId == u.UserId) +
                                 _context.CommentPosts.Count(c => c.UserId == u.UserId)
                })
                .OrderByDescending(u => u.DocumentCount + u.TopicCount + u.PostCount + u.CommentCount)
                .Take(5)
                .ToListAsync();

            // Most used tags
            var mostUsedTags = await _context.Tags
                .Select(t => new
                {
                    t.Name,
                    UsageCount = t.TopicTags.Count
                })
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .ToListAsync();

            // Prepare tag data for chart
            var tagNames = mostUsedTags.Select(t => t.Name).ToList();
            var tagCounts = mostUsedTags.Select(t => t.UsageCount).ToList();

            // Tag statistics
            var tagStats = new {
                TotalTags = await _context.Tags.CountAsync(),
                UsedTags = await _context.TopicTags.Select(tt => tt.TagId).Distinct().CountAsync(),
                AverageTagsPerTopic = await _context.Topics.AnyAsync()
                    ? await _context.TopicTags.CountAsync() / (double)await _context.Topics.CountAsync()
                    : 0
            };

            // Engagement statistics
            var engagementStats = new {
                TotalLikes = await _context.LikeDocuments.CountAsync() + await _context.LikeTopics.CountAsync() + await _context.LikePosts.CountAsync(),
                DocumentLikes = await _context.LikeDocuments.CountAsync(),
                TopicLikes = await _context.LikeTopics.CountAsync(),
                PostLikes = await _context.LikePosts.CountAsync()
            };

            // Activity chart data for last 30 days
            var days = Enumerable.Range(0, 30)
                .Select(i => DateTime.Now.Date.AddDays(-29 + i))
                .ToList();

            var documentActivity = new List<int>();
            var topicActivity = new List<int>();
            var commentActivity = new List<int>();

            foreach (var day in days)
            {
                documentActivity.Add(await _context.Documents.CountAsync(d => d.CreatedAt.HasValue && d.CreatedAt.Value.Date == day));
                topicActivity.Add(await _context.Topics.CountAsync(t => t.CreatedAt.HasValue && t.CreatedAt.Value.Date == day));
                commentActivity.Add(
                    await _context.Comments.CountAsync(c => c.CreatedAt.Date == day) +
                    await _context.CommentDocuments.CountAsync(c => c.CreatedAt.Date == day) +
                    await _context.CommentPosts.CountAsync(c => c.CreatedAt.Date == day)
                );
            }

            ViewBag.UserStats = userStats;
            ViewBag.ContentStats = contentStats;
            ViewBag.GroupStats = groupStats;
            ViewBag.CommentStats = commentStats;
            ViewBag.ActivityStats = activityStats;
            ViewBag.MostActiveUsers = mostActiveUsers;
            ViewBag.MostUsedTags = mostUsedTags;
            ViewBag.TagNames = tagNames;
            ViewBag.TagCounts = tagCounts;
            ViewBag.TagStats = tagStats;
            ViewBag.EngagementStats = engagementStats;
            ViewBag.ActivityDates = days.Select(d => d.ToString("dd/MM")).ToList();
            ViewBag.DocumentActivity = documentActivity;
            ViewBag.TopicActivity = topicActivity;
            ViewBag.CommentActivity = commentActivity;

            return View();
        }
    }
} 