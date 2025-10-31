using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleAuthLog.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        public async Task OnGetAsync()
        {
            AuditLogs = await _context.AuditLogs
                                    .OrderByDescending(log => log.Timestamp)
                                    .Take(10)
                                    .ToListAsync();
        }
    }
}