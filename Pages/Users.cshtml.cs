using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;

namespace SimpleAuthLog.Pages
{
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<User> UserList { get; set; } = new List<User>();

        public async Task OnGetAsync()
        {
            UserList = await _context.Users.ToListAsync();
        }
    }
}