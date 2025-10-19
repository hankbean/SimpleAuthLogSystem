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

        // ô���ݩʡA�Ω󱵦������
        [BindProperty]
        public required UserDto NewUser { get; set; }

        // �Ω���ܤ�x
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // �Ω���ܰT��
        public required string Message { get; set; }

        // �������J�� (GET�ШD)
        public async Task OnGetAsync()
        {
            // �q��ƮwŪ����x��ƨ����
            AuditLogs = await _context.AuditLogs.OrderByDescending(log => log.Timestamp).Take(10).ToListAsync();
        }

        // ���洣��� (POST�ШD)
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // �إߤ@�� HttpClient �өI�s�ڭ̪� Web API
            var client = _clientFactory.CreateClient();

            // �ǳƭn POST �����
            var userJson = new StringContent(
                JsonSerializer.Serialize(NewUser),
                Encoding.UTF8,
                "application/json");

            // ���o API �� URL (�ܭ��n�A�ݭn�]�t http/https �M port)
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Request.Scheme}://{Request.Host}/api/users");
            request.Content = userJson;

            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Message = "�ϥΪ̷s�W���\�I";
            }
            else
            {
                Message = "�s�W���ѡA�Ьd�� API ��X�C";
            }

            // ���s�ɦV�^�����H��s��x
            return RedirectToPage();
        }
    }

    // �ڭ̥i�H���� API Controller �� DTO
    // public class UserDto
    // {
    //     public string Username { get; set; }
    //     public string Password { get; set; }
    // }
}