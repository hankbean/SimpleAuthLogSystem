using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimpleAuthLog.Controllers;
using SimpleAuthLog.Data;
using SimpleAuthLog.DTOs;
using SimpleAuthLog.Models;
using SimpleAuthLog.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SimpleAuthLog.Pages
{
    [Authorize(Roles = "Admin")]
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly IAuditService _auditService;

        public RolesModel(ApplicationDbContext context,
                  RoleManager<IdentityRole<int>> roleManager,
                  UserManager<IdentityUser<int>> userManager,
                  IAuditService auditService)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        public IList<IdentityRole<int>> RoleList { get; set; } = new List<IdentityRole<int>>();

        [BindProperty]
        public RoleDto NewRole { get; set; } = new();

        [BindProperty]
        public RoleDto EditRole { get; set; } = new();

        [BindProperty]
        public int EditRoleId { get; set; }

        public async Task OnGetAsync()
        {
            RoleList = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(NewRole, nameof(NewRole)) || string.IsNullOrWhiteSpace(NewRole.RoleName))
            {
                TempData["ErrorMessage"] = "建立失敗: 角色名稱為必填且不可為空白。";
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "建立失敗: 提交的資料無效。";
                return RedirectToPage(); 
            }

            if (await _roleManager.RoleExistsAsync(NewRole.RoleName))
            {
                TempData["ErrorMessage"] = $"建立失敗: 角色 '{NewRole.RoleName}' 已經存在。";
                return RedirectToPage(); 
            }

            var newRole = new IdentityRole<int> { Name = NewRole.RoleName };
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _roleManager.CreateAsync(newRole);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "建立失敗: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{newRole.Name}' (ID: {newRole.Id}) 已被建立");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "角色建立成功！";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "建立時發生嚴重錯誤: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "刪除失敗: 找不到該角色";
                return RedirectToPage();
            }

            var roleName = role.Name;
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "刪除失敗: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{roleName}' (ID: {id}) 已被刪除");
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "角色刪除成功！";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "刪除時發生嚴重錯誤: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(EditRole, nameof(EditRole)))
            {
                TempData["ErrorMessage"] = "更新失敗: 角色名稱為必填。";
                return RedirectToPage();
            }
            var role = await _roleManager.FindByIdAsync(EditRoleId.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "更新失敗: 找不到該角色";
                return RedirectToPage();
            }

            var oldName = role.Name;
            role.Name = EditRole.RoleName;
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "更新失敗: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"角色 '{oldName}' (ID: {role.Id}) 已被更新為 '{role.Name}'");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "角色更新成功！";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "更新時發生嚴重錯誤: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}