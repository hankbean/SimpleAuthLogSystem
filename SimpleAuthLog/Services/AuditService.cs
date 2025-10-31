using SimpleAuthLog.Data;
using SimpleAuthLog.Models;

namespace SimpleAuthLog.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        
        public ApplicationDbContext Context => _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void LogAction(int userId, string action)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
        }
    }
}