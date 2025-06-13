using Microsoft.AspNetCore.Identity;

namespace Weixin_Project
{
    public class InitData
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration configuration;

        public InitData(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.configuration = configuration;
        }

        public async Task InitAsync()
        {
            // 创建角色
            await CreateRolesAsync();

            // 创建管理员用户
            await CreateAdminUserAsync();
        }

        private async Task CreateRolesAsync()
        {
            string[] roles = { "Admin", "User" };

            foreach (var roleName in roles)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"创建角色: {roleName}");
                }
            }
        }

        private async Task CreateAdminUserAsync()
        {
            if (await userManager.FindByNameAsync("admin") == null)
            {
                IdentityUser adminUser = new IdentityUser
                {
                    UserName = "admin"
                };

                var createResult = await userManager.CreateAsync(adminUser, "123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"创建管理员用户: {adminUser.UserName}");
                }
                else
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    Console.WriteLine($"创建管理员用户失败: {errors}");
                }
            }
        }
    }
}
