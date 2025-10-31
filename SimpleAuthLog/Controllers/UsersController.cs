using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.DTOs;
using SimpleAuthLog.Models;
using SimpleAuthLog.Services;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace SimpleAuthLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public UsersController(
            UserManager<IdentityUser<int>> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IAuditService auditService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _auditService = auditService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdentityUser<int>>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IdentityUser<int>>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutUser(int id, UserUpdateDto userUpdateDto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound($"找不到 ID 為 {id} 的使用者");
            }

            var oldUsername = user.UserName;

            if (!string.IsNullOrEmpty(userUpdateDto.Username))
            {
                user.UserName = userUpdateDto.Username;
            }

            if (!string.IsNullOrEmpty(userUpdateDto.Password))
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, userUpdateDto.Password);
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{oldUsername}' (ID: {id}) 的資料已被更新");
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound($"找不到 ID 為 {id} 的使用者");
            }

            var deletedUsername = user.UserName;

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{deletedUsername}' (ID: {id}) 已被刪除");
                return NoContent();
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("{id}/assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRoleToUser(int id, [FromBody] RoleDto roleDto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { Message = $"找不到 ID 為 {id} 的使用者" });
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleDto.RoleName);
            if (!roleExists)
            {
                return BadRequest(new { Message = $"角色 '{roleDto.RoleName}' 不存在" });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var result = await _userManager.AddToRoleAsync(user, roleDto.RoleName);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(result.Errors);
                }

                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{user.UserName}' (ID: {id}) 已被指派 '{roleDto.RoleName}' 角色");

                await _auditService.Context.SaveChangesAsync(); 
                await transaction.CommitAsync();
                return Ok(new { Message = $"已成功將 '{roleDto.RoleName}' 角色指派給 '{user.UserName}'" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { Message = "儲存稽核日誌時發生錯誤", Error = ex.Message });
            }
        }
    }

}