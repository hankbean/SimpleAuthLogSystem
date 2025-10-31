using SimpleAuthLog.Data;

namespace SimpleAuthLog.Services
{
    public interface IAuditService
    {
        ApplicationDbContext Context { get; }
        void LogAction(int userId, string action);
    }
}