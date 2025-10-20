using System.ComponentModel.DataAnnotations; 

namespace SimpleAuthLog.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }
}