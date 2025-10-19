using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Controllers;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;
using System.Text; // For StringContent
using System.Text.Json; // For JsonSerializer

namespace SimpleAuthLog.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public IndexModel(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        // 繫結屬性，用於接收表單資料
        [BindProperty]
        public required UserDto NewUser { get; set; }

        // 用於顯示日誌
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // 用於顯示訊息
        public required string Message { get; set; }

        // 當頁面載入時 (GET請求)
        public async Task OnGetAsync()
        {
            // 從資料庫讀取日誌資料並顯示
            AuditLogs = await _context.AuditLogs.OrderByDescending(log => log.Timestamp).Take(10).ToListAsync();
        }

        // 當表單提交時 (POST請求)
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 建立一個 HttpClient 來呼叫我們的 Web API
            var client = _clientFactory.CreateClient();

            // 準備要 POST 的資料
            var userJson = new StringContent(
                JsonSerializer.Serialize(NewUser),
                Encoding.UTF8,
                "application/json");

            // 取得 API 的 URL (很重要，需要包含 http/https 和 port)
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Request.Scheme}://{Request.Host}/api/users");
            request.Content = userJson;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Message = "使用者新增成功！";
            }
            else
            {
                Message = "新增失敗，請查看 API 輸出。";
            }

            // 重新導向回頁面以刷新日誌
            return RedirectToPage();
        }
    }

    // 我們可以重用 API Controller 的 DTO
    // public class UserDto
    // {
    //     public string Username { get; set; }
    //     public string Password { get; set; }
    // }
}