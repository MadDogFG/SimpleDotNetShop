// Weixin_Project\Controllers\AdminUserController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Weixin_Project.DTOs; // 假设您有UserViewModel和UserUpdateRequestDto
using Weixin_Project.Utils; // For PagedResponse

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // 只有管理员才能访问这些接口
    public class AdminUserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext; // 用于访问其他相关数据，如订单、地址等

        public AdminUserController(UserManager<IdentityUser> userManager, ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取所有用户列表（带分页和筛选）
        /// </summary>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="searchTerm">搜索关键词（用户名或邮箱）</param>
        /// <param name="includeAdmin">是否包含管理员用户</param>
        /// <returns>分页用户列表</returns>
        [HttpGet]
        public async Task<ActionResult<PagedResponse<UserViewModel>>> GetUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool includeAdmin = false) // 默认不包含管理员
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // 限制最大每页数量

            // 获取所有非管理员用户，或者根据 includeAdmin 参数决定是否包含管理员
            IQueryable<IdentityUser> query = _userManager.Users;

            if (!includeAdmin)
            {
                // 排除管理员角色用户
                var adminRole = await _userManager.GetUsersInRoleAsync("Admin");
                var adminUserIds = adminRole.Select(u => u.Id).ToList();
                query = query.Where(u => !adminUserIds.Contains(u.Id));
            }

            // 搜索功能：按用户名或邮箱模糊搜索
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u =>
                    u.UserName!.Contains(searchTerm) ||
                    (u.Email != null && u.Email.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.LockoutEnd) // 锁定用户排在前面
                .ThenByDescending(u => u.UserName)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = users.Select(u => new UserViewModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                LockoutEnabled = u.LockoutEnabled,
                LockoutEnd = u.LockoutEnd, // 锁定结束时间
                IsLockedOut = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow, // 是否被锁定
                AccessFailedCount = u.AccessFailedCount,
                Roles = _userManager.GetRolesAsync(u).Result.ToList() // 同步获取角色，这里为了简化
            }).ToList();

            var response = new PagedResponse<UserViewModel>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = userViewModels,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }

        /// <summary>
        /// 获取单个用户详情
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>用户详情</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户未找到。" });
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                AccessFailedCount = user.AccessFailedCount,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };

            return Ok(userViewModel);
        }


        /// <summary>
        /// 禁用/锁定用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>操作结果</returns>
        [HttpPut("LockUser/{id}")]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户未找到。" });
            }

            // 检查是否是管理员，不允许锁定管理员自己或其它管理员
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return BadRequest(new { message = "不能锁定管理员账户。" });
            }

            // 锁定用户100年，等同于永久锁定
            var lockResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!lockResult.Succeeded)
            {
                return BadRequest(new { message = $"锁定用户失败: {string.Join(", ", lockResult.Errors.Select(e => e.Description))}" });
            }

            return Ok(new { message = $"用户 '{user.UserName}' 已成功锁定。" });
        }

        /// <summary>
        /// 启用/解锁用户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>操作结果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户未找到。" });
            }

            // 解锁用户
            var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null); // 设置为null表示解锁
            if (!unlockResult.Succeeded)
            {
                return BadRequest(new { message = $"解锁用户失败: {string.Join(", ", unlockResult.Errors.Select(e => e.Description))}" });
            }

            return Ok(new { message = $"用户 '{user.UserName}' 已成功解锁。" });
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="request">重置密码请求DTO</param>
        /// <returns>操作结果</returns>
        [HttpPost("{id}")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "用户未找到。" });
            }

            // 检查是否是管理员，不允许重置管理员密码（出于安全考虑，管理员密码应通过其他方式管理）
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return BadRequest(new { message = "不能重置管理员账户的密码。" });
            }

            // 移除所有现有密码（如果有的话）
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded && removePasswordResult.Errors.Any(e => e.Code != "PasswordNotSet"))
            {
                return BadRequest(new { message = $"移除旧密码失败: {string.Join(", ", removePasswordResult.Errors.Select(e => e.Description))}" });
            }

            // 添加新密码
            var addPasswordResult = await _userManager.AddPasswordAsync(user, request.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                return BadRequest(new { message = $"设置新密码失败: {string.Join(", ", addPasswordResult.Errors.Select(e => e.Description))}" });
            }

            return Ok(new { message = $"用户 '{user.UserName}' 密码已成功重置。" });
        }
    }
}