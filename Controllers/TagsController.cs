using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class TagsController : BaseController
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Tags
        public async Task<IActionResult> Index(string searchString)
        {
            var tagsQuery = _context.Tags
                .Include(t => t.TopicTags)
                .ThenInclude(tt => tt.Topic)
                .AsQueryable();

            // Tìm kiếm nếu có searchString
            if (!string.IsNullOrEmpty(searchString))
            {
                tagsQuery = tagsQuery.Where(t => t.Name.Contains(searchString));
            }

            var tags = await tagsQuery.ToListAsync();

            // Tạo Dictionary để lưu trữ số lượng topic cho mỗi tag
            var tagWithTopicCount = tags.ToDictionary(
                tag => tag.TagId,
                tag => tag.TopicTags?.Count() ?? 0
            );

            // Truyền thông tin vào ViewBag
            ViewBag.TagWithTopicCount = tagWithTopicCount;

            return View(tags);
        }


        // GET: Tags/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.TopicTags)
                .ThenInclude(tt => tt.Topic)
                .FirstOrDefaultAsync(m => m.TagId == id);

            if (tag == null)
            {
                return NotFound();
            }

            // Lấy danh sách các topic để hiển thị trong form thêm topic
            ViewBag.Topics = await _context.Topics.ToListAsync();

            return View(tag);
        }


        // POST: Tags/AddTopic/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTopics(int id, List<int> topicIds)
        {
            var tag = await _context.Tags
                .Include(t => t.TopicTags)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null)
            {
                return NotFound();
            }

            foreach (var topicId in topicIds)
            {
                // Kiểm tra nếu topic chưa được liên kết với tag
                if (!_context.TopicTags.Any(tt => tt.TagId == id && tt.TopicId == topicId))
                {
                    // Tạo mối liên kết giữa tag và topic
                    _context.TopicTags.Add(new TopicTag
                    {
                        TagId = id,
                        TopicId = topicId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = tag.TagId });
        }

        // POST: Tags/RemoveTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTopic(int tagId, int topicId)
        {
            var tag = await _context.Tags
                .Include(t => t.TopicTags)
                .FirstOrDefaultAsync(t => t.TagId == tagId);

            if (tag == null)
            {
                return NotFound();
            }

            var topicTag = await _context.TopicTags
                .FirstOrDefaultAsync(tt => tt.TagId == tagId && tt.TopicId == topicId);

            if (topicTag != null)
            {
                _context.TopicTags.Remove(topicTag);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = tagId });
        }


        // GET: Tags/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tags/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TagId,Name")] Tag tag)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tag);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags.FindAsync(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }

        // POST: Tags/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TagId,Name")] Tag tag)
        {
            if (id != tag.TagId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tag);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TagExists(tag.TagId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tag);
        }

        // GET: Tags/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .FirstOrDefaultAsync(m => m.TagId == id);
            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }

        // POST: Tags/Delete/5
        // POST: Tags/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSelected(int[] selectedTags)
        {
            if (selectedTags == null || selectedTags.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tag để xóa.";
                return RedirectToAction("Index");
            }

            var tagsToDelete = _context.Tags
                .Where(t => selectedTags.Contains(t.TagId))
                .Include(t => t.TopicTags) // Bao gồm các TopicTags liên quan
                .ToList();

            foreach (var tag in tagsToDelete)
            {
                // Xóa các liên kết trong bảng TopicTags
                _context.TopicTags.RemoveRange(tag.TopicTags);

                // Xóa Tag
                _context.Tags.Remove(tag);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa các tag được chọn.";
            return RedirectToAction("Index");
        }

        private bool TagExists(int id)
        {
            return _context.Tags.Any(e => e.TagId == id);
        }
    }
}
