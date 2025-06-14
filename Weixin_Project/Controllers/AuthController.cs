using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Weixin_Project.DTOs;

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;
        private readonly ILogger<AuthController> logger;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, ILogger<AuthController> logger)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            try
            {
                // 查找用户
                var user = await userManager.FindByNameAsync(loginRequest.Username);
                if (user == null)
                {
                    logger.LogWarning($"登录失败: 用户 {loginRequest.Username} 不存在");
                    return BadRequest(new { message = "用户名或密码错误" });
                }

                // 验证密码
                var result = await userManager.CheckPasswordAsync(user, loginRequest.Password);
                if (!result)
                {
                    logger.LogWarning($"登录失败: 用户 {loginRequest.Username} 密码错误");
                    return BadRequest(new { message = "用户名或密码错误" });
                }

                // 获取用户角色
                var roles = await userManager.GetRolesAsync(user);

                // 生成JWT
                var token = GenerateJwtToken(user, roles);

                logger.LogInformation($"用户 {loginRequest.Username} 登录成功");
                return Ok(new { token, username = user.UserName, roles });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "登录过程中发生错误");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequest)
        {
            try
            {
                // 检查用户名是否已存在
                var existingUser = await userManager.FindByNameAsync(registerRequest.Username);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "用户名已被使用" });
                }

                // 创建新用户
                var user = new IdentityUser
                {
                    UserName = registerRequest.Username
                };

                var createResult = await userManager.CreateAsync(user, registerRequest.Password);

                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { errors });
                }

                // 默认分配用户角色
                await userManager.AddToRoleAsync(user, "User");

                logger.LogInformation($"新用户注册: {registerRequest.Username}");

                // 注册成功后返回成功消息
                return Ok(new { message = "注册成功，请登录" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "注册过程中发生错误");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
        };

            // 添加角色声明
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(configuration["Jwt:ExpireMinutes"]));

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
