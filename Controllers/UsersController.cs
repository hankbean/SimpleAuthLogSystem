using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;
using System.Security.Cryptography;
using System.Text;
using SimpleAuthLog.DTOs;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(UserDto userDto)
        {
            var passwordHash = HashPassword(userDto.Password);

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await LogAction(user.Id, $"使用者 '{user.Username}' 已被建立");

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UserUpdateDto userUpdateDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 {id} 的使用者");
            }

            var oldUsername = user.Username;

            if (!string.IsNullOrEmpty(userUpdateDto.Username))
            {
                user.Username = userUpdateDto.Username;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.Password))
            {
                user.PasswordHash = HashPassword(userUpdateDto.Password);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await LogAction(id, $"使用者 '{oldUsername}' (ID: {id}) 的資料已被更新。新使用者名稱: '{user.Username}'");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound($"找不到 ID 為 {id} 的使用者");
            }

            var deletedUsername = user.Username;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await LogAction(0, $"使用者 '{deletedUsername}' (ID: {id}) 已被刪除");

            return NoContent();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private async Task LogAction(int userId, string action)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }

}