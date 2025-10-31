using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Data;
using SimpleAuthLog.DTOs;
using SimpleAuthLog.Models;
using SimpleAuthLog.Services;
using System.Security.Claims;

namespace SimpleAuthLog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IAuditService _auditService;

        public RolesController(RoleManager<IdentityRole<int>> roleManager, IAuditService auditService)
        {
            _roleManager = roleManager;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdentityRole<int>>>> GetRoles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IdentityRole<int>>> GetRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());

            if (role == null)
            {
                return NotFound();
            }

            return role;
        }

        [HttpPost]
        public async Task<ActionResult<IdentityRole<int>>> PostRole(RoleDto roleDto)
        {
            var role = new IdentityRole<int>
            {
                Name = roleDto.RoleName
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{role.Name}' 已被建立");
                return CreatedAtAction(nameof(GetRole), "Roles", new { id = role.Id }, role);
            }

            return BadRequest(result.Errors);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, RoleDto roleDto)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound($"找不到 ID 為 {id} 的角色");
            }

            var oldRoleName = role.Name;
            role.Name = roleDto.RoleName;

            await _roleManager.UpdateAsync(role);

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{oldRoleName}' (ID: {id}) 已被更新為 '{role.Name}'");
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound($"找不到 ID 為 {id} 的角色");
            }

            var deletedRoleName = role.Name;

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{deletedRoleName}' (ID: {id}) 已被刪除");
                return NoContent();
            }

            return BadRequest(result.Errors);
        }
    }

  
}