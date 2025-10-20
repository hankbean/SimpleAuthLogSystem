using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.Models;

namespace SimpleAuthLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            return role;
        }

        [HttpPost]
        public async Task<ActionResult<Role>> PostRole(RoleDto roleDto)
        {
            var role = new Role
            {
                RoleName = roleDto.RoleName
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // 先暫時假設操作者是系統 (UserId = 0)，
            // 在有登入機制後，這裡應傳入當前登入使用者的 ID。
            await LogAction(0, $"角色 '{role.RoleName}' 已被建立");

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, RoleDto roleDto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound($"找不到 ID 為 {id} 的角色");
            }

            var oldRoleName = role.RoleName;
            role.RoleName = roleDto.RoleName;

            _context.Entry(role).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await LogAction(0, $"角色 '{oldRoleName}' (ID: {id}) 已被更新為 '{role.RoleName}'");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Roles.Any(e => e.Id == id))
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
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound($"找不到 ID 為 {id} 的角色");
            }

            var deletedRoleName = role.RoleName;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            await LogAction(0, $"角色 '{deletedRoleName}' (ID: {id}) 已被刪除");

            return NoContent();
        }
    }

    public class RoleDto
    {
        public required string RoleName { get; set; }
    }
}