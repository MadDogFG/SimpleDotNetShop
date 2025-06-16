
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using FluentValidation;
using System.Reflection;
using System.Text;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using Weixin_Project.Utils;

namespace Weixin_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 添加数据库上下文
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // 配置Identity
            builder.Services.AddDataProtection();//数据保护
            builder.Services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 5;//最大失败次数
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);//锁定时间
                options.Password.RequireDigit = false;//是否需要数字
                options.Password.RequireLowercase = false;//是否需要小写字母
                options.Password.RequireUppercase = false; //是否需要大写字母
                options.Password.RequireNonAlphanumeric = false;//是否需要特殊字符
                options.Password.RequiredLength = 6;//密码长度
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            // 配置JWT认证
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                };
            });

            // 添加数据初始化服务
            builder.Services.AddTransient<InitData, InitData>();

            // 数据校验
            builder.Services.AddFluentValidationAutoValidation();//WebApi自动添加验证
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());//从当前程序集加载所有的验证器

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //配置跨域
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            builder.Services.AddSwaggerGen(c =>
            {
                var scheme = new OpenApiSecurityScheme()
                {
                    Description = "Authorization header. \r\nExample: 'Bearer 12345abcdef'",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Authorization"
                    },
                    Scheme = "oauth2",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                };
                c.AddSecurityDefinition("Authorization", scheme);
                var requirement = new OpenApiSecurityRequirement();
                requirement[scheme] = new List<string>();
                c.AddSecurityRequirement(requirement);
            });


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors();//必须在UseHttpsRedirection前面
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // 初始化数据
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var initializer = services.GetRequiredService<InitData>();
                    await initializer.InitAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("初始化错误："+ex.ToString());
                }
            }

            app.Run();
        }
    }
}
