using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class BaseController : Controller
    {
        public string CurrentUserID
        {
            get
            {
                // Đọc từ sesion 
                return HttpContext.Session.GetString("USERID");
            }
            set
            {
                // Gán dữ liệu cho session
                HttpContext.Session.SetString("USERID", value);
            }
        }
        public string CurrentUserName
        {
            get
            {
                // Đọc từ sesion 
                return HttpContext.Session.GetString("USERNAME");
            }
            set
            {
                // Gán dữ liệu cho session
                HttpContext.Session.SetString("USERNAME", value);
            }
        }
     
        public string CurrentUserRole
        {
            get
            {
                // Doc tu session
                return HttpContext.Session.GetString("ROLE");
            }
            set
            {
                HttpContext.Session.SetString("ROLE", value);
            }
        }
        public bool IsLogin
        {
            get
            {
                return !string.IsNullOrEmpty(CurrentUserID);
            }
        }
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ViewBag.IsLogin = IsLogin;
            ViewBag.Role = CurrentUserRole;
            ViewBag.CurrentUserName = CurrentUserName;
            ViewBag.CurrentUserId = CurrentUserID;
            base.OnActionExecuted(filterContext);
        }
        protected string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        public static string ToSimpleUsername(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "";
            string normalized = fullName.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                    sb.Append(c);
                }
            }
            return sb.ToString().Replace("Đ", "D").Replace("đ", "d").ToLower();
        }
    }
}