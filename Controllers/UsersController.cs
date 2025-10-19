using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;
using System.Security.Cryptography; // 用於密碼雜湊
using System.Text; // 用於密碼雜湊

namespace SimpleAuthLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users - 查詢所有使用者
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // POST: api/users - 新增使用者
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(UserDto userDto)
        {
            // 1. 密碼雜湊處理
            var passwordHash = HashPassword(userDto.Password);

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = passwordHash
            };

            // 2. 存入資料庫
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 3. 寫入 Audit Log
            await LogAction(user.Id, $"使用者 '{user.Username}' 已被建立");

            // 使用 CreatedAtAction 回傳 201 Created 狀態碼，更符合 RESTful 風格
            return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
        }

        // 密碼雜湊函式 (知識點)
        private string HashPassword(string password)
        {
            // 這是一個非常簡化的範例，真實世界請使用 BCrypt 或 Identity 框架
            // 這裡沒有加鹽 (Salt)，僅為演示雜湊概念
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // 日誌記錄共用函式 (知識點)
        private async Task LogAction(int userId, string action)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow // 使用 UTC 時間是好習慣
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }

    // DTO (Data Transfer Object) - 用於接收來自前端的資料
    public class UserDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}