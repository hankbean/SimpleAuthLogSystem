namespace SimpleAuthLog.Models
{
    // 多對多關聯的對應表
    public class UserRole
    {
        public int UserId { get; set; }
        public required User User { get; set; } // 導覽屬性

        public int RoleId { get; set; }
        public required Role Role { get; set; } // 導覽屬性
    }
}