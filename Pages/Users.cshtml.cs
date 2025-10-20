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
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public UsersModel(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        public IList<User> UserList { get; set; } = new List<User>();

        [BindProperty]
        public UserUpdateDto EditUser { get; set; } = new();

        [BindProperty]
        public int EditUserId { get; set; }


        public async Task OnGetAsync()
        {
            UserList = await _context.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var client = _clientFactory.CreateClient();
            var response = await client.DeleteAsync($"{Request.Scheme}://{Request.Host}/api/users/{id}");

            return RedirectToPage(); 
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var client = _clientFactory.CreateClient();
            var userJson = new StringContent(
                JsonSerializer.Serialize(EditUser),
                Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync($"{Request.Scheme}://{Request.Host}/api/users/{EditUserId}", userJson);

            return RedirectToPage();
        }
    }
}