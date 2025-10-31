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
using static System.Net.Mime.MediaTypeNames;

namespace SimpleAuthLog.Pages
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<IdentityUser<int>> _userManager;
        private readonly IAuditService _auditService;

        public UsersModel(ApplicationDbContext context,
            IHttpClientFactory clientFactory,
            RoleManager<IdentityRole<int>> roleManager,
            UserManager<IdentityUser<int>> userManager,
            IAuditService auditService)
        {
            _context = context;
            _clientFactory = clientFactory;
            _roleManager = roleManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        public IList<UserWithRolesViewModel> UserListWithRoles { get; set; } = new List<UserWithRolesViewModel>();

        public IList<IdentityRole<int>> RoleList { get; set; } = new List<IdentityRole<int>>();

        [BindProperty]
        public UserUpdateDto EditUser { get; set; } = new();

        [BindProperty]
        public int EditUserId { get; set; }

        [BindProperty]
        public int AssignRoleUserId { get; set; }
        [BindProperty]
        public string AssignRoleName { get; set; }


        public async Task OnGetAsync()
        {
            RoleList = await _roleManager.Roles.AsNoTracking().ToListAsync();

            var users = await _context.Users.AsNoTracking().ToListAsync();
            UserListWithRoles = new List<UserWithRolesViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                UserListWithRoles.Add(new UserWithRolesViewModel
                {
                    User = user,
                    Roles = roles
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = $"刪除失敗: 找不到 ID 為 {id} 的使用者";
                return RedirectToPage();
            }

            var deletedUsername = user.UserName;
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "刪除失敗: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{deletedUsername}' (ID: {id}) 已被刪除");

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "使用者刪除成功！";
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
            var user = await _userManager.FindByIdAsync(this.EditUserId.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = $"更新失敗: 找不到 ID 為 {EditUserId} 的使用者";
                return RedirectToPage();
            }

            var oldUsername = user.UserName;
            bool hasChanges = false;

            if (!string.IsNullOrEmpty(EditUser.Username) && user.UserName != EditUser.Username)
            {
                hasChanges = true;
            }
            if (!string.IsNullOrEmpty(EditUser.Password))
            {
                hasChanges = true;
            }

            if (hasChanges)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (!string.IsNullOrEmpty(EditUser.Username) && user.UserName != EditUser.Username)
                    {
                        user.UserName = EditUser.Username;
                        await _userManager.UpdateAsync(user); 
                    }

                    if (!string.IsNullOrEmpty(EditUser.Password))
                    {
                        await _userManager.RemovePasswordAsync(user);
                        await _userManager.AddPasswordAsync(user, EditUser.Password); 
                    }

                    var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{oldUsername}' (ID: {user.Id}) 的資料已被更新");

                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "使用者更新成功！";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "更新時發生嚴重錯誤: " + ex.Message;
                }
            }
            else
            {
                TempData["SuccessMessage"] = "沒有偵測到任何變更。";
            }

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostAssignRoleAsync()
        {
            var user = await _userManager.FindByIdAsync(this.AssignRoleUserId.ToString());
            if (user == null) 
            {
                TempData["ErrorMessage"] = "建立失敗: 角色名稱為必填。";
                return RedirectToPage();
            }
            var roleExists = await _roleManager.RoleExistsAsync(this.AssignRoleName);
            if (!roleExists) 
            {
                TempData["ErrorMessage"] = $"建立失敗: 角色 '{this.AssignRoleName}' 已經存在。";
                return RedirectToPage();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.AddToRoleAsync(user, this.AssignRoleName);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"指派失敗: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    return RedirectToPage();
                }

                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{user.UserName}' (ID: {user.Id}) 已被管理員 (ID: {adminUserId}) 指派 '{this.AssignRoleName}' 角色");

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "角色指派成功！";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"指派過程中發生嚴重錯誤: {ex.Message}";
            }

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostRemoveRoleAsync(int userid, string rolename)
        {
            var user = await _userManager.FindByIdAsync(userid.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = $"移除角色失敗: 找不到 ID 為 {userid} 的使用者";
                return RedirectToPage();
            }

            if (!await _userManager.IsInRoleAsync(user, rolename))
            {
                TempData["ErrorMessage"] = $"移除角色失敗: 使用者 '{user.UserName}' 並沒有 '{rolename}' 角色";
                return RedirectToPage();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.RemoveFromRoleAsync(user, rolename);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "移除角色失敗: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _auditService.LogAction(Convert.ToInt32(adminUserId), $"使用者 '{user.UserName}' (ID: {user.Id}) 的 '{rolename}' 角色已被移除");

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "角色移除成功！";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "移除角色時發生嚴重錯誤: " + ex.Message;
            }

            return RedirectToPage();
        }
    }

    public class UserWithRolesViewModel
    {
        public IdentityUser<int> User { get; set; }
        public IList<string> Roles { get; set; }
    }
}