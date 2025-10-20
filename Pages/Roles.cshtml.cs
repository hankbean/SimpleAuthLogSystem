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
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public RolesModel(ApplicationDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        public IList<Role> RoleList { get; set; } = new List<Role>();

        [BindProperty]
        public RoleDto NewRole { get; set; } = new();

        [BindProperty]
        public RoleDto EditRole { get; set; } = new();

        [BindProperty]
        public int EditRoleId { get; set; }

        public async Task OnGetAsync()
        {
            RoleList = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                RoleList = await _context.Roles.ToListAsync();
                return Page();
            }

            var client = _clientFactory.CreateClient();
            var roleJson = new StringContent(
                JsonSerializer.Serialize(NewRole),
                Encoding.UTF8,
                "application/json");

            await client.PostAsync($"{Request.Scheme}://{Request.Host}/api/roles", roleJson);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var client = _clientFactory.CreateClient();
            await client.DeleteAsync($"{Request.Scheme}://{Request.Host}/api/roles/{id}");
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var client = _clientFactory.CreateClient();
            var roleJson = new StringContent(
                JsonSerializer.Serialize(EditRole),
                Encoding.UTF8,
                "application/json");

            await client.PutAsync($"{Request.Scheme}://{Request.Host}/api/roles/{EditRoleId}", roleJson);
            return RedirectToPage();
        }
    }
}