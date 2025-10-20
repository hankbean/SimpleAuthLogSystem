using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Controllers;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;
using System.Text; 
using System.Text.Json;
using SimpleAuthLog.DTOs;

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

        [BindProperty]
        public required UserDto NewUser { get; set; }

        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public required string Message { get; set; }

        public async Task OnGetAsync()
        {
            AuditLogs = await _context.AuditLogs.OrderByDescending(log => log.Timestamp).Take(10).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _clientFactory.CreateClient();

            var userJson = new StringContent(
                JsonSerializer.Serialize(NewUser),
                Encoding.UTF8,
                "application/json");

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

            return RedirectToPage();
        }
    }
}