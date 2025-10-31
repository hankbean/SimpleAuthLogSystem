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
                TempData["ErrorMessage"] = "�إߥ���: ����W�٬�����B���i���ťաC";
                return RedirectToPage();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "�إߥ���: ���檺��ƵL�ġC";
                return RedirectToPage(); 
            }

            if (await _roleManager.RoleExistsAsync(NewRole.RoleName))
            {
                TempData["ErrorMessage"] = $"�إߥ���: ���� '{NewRole.RoleName}' �w�g�s�b�C";
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
                    TempData["ErrorMessage"] = "�إߥ���: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"���� '{newRole.Name}' (ID: {newRole.Id}) �w�Q�إ�");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "����إߦ��\�I";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "�إ߮ɵo���Y�����~: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "�R������: �䤣��Ө���";
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
                    TempData["ErrorMessage"] = "�R������: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"���� '{roleName}' (ID: {id}) �w�Q�R��");
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "����R�����\�I";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "�R���ɵo���Y�����~: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(EditRole, nameof(EditRole)))
            {
                TempData["ErrorMessage"] = "��s����: ����W�٬�����C";
                return RedirectToPage();
            }
            var role = await _roleManager.FindByIdAsync(EditRoleId.ToString());
            if (role == null)
            {
                TempData["ErrorMessage"] = "��s����: �䤣��Ө���";
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
                    TempData["ErrorMessage"] = "��s����: " + string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToPage();
                }

                _auditService.LogAction(Convert.ToInt32(adminUserId), $"���� '{oldName}' (ID: {role.Id}) �w�Q��s�� '{role.Name}'");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "�����s���\�I";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "��s�ɵo���Y�����~: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}