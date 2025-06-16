
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

            // ������ݿ�������
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // ����Identity
            builder.Services.AddDataProtection();//���ݱ���
            builder.Services.AddIdentityCore<IdentityUser>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 5;//���ʧ�ܴ���
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);//����ʱ��
                options.Password.RequireDigit = false;//�Ƿ���Ҫ����
                options.Password.RequireLowercase = false;//�Ƿ���ҪСд��ĸ
                options.Password.RequireUppercase = false; //�Ƿ���Ҫ��д��ĸ
                options.Password.RequireNonAlphanumeric = false;//�Ƿ���Ҫ�����ַ�
                options.Password.RequiredLength = 6;//���볤��
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            // ����JWT��֤
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

            // ������ݳ�ʼ������
            builder.Services.AddTransient<InitData, InitData>();

            // ����У��
            builder.Services.AddFluentValidationAutoValidation();//WebApi�Զ������֤
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());//�ӵ�ǰ���򼯼������е���֤��

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //���ÿ���
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
            app.UseCors();//������UseHttpsRedirectionǰ��
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // ��ʼ������
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
                    Console.WriteLine("��ʼ������"+ex.ToString());
                }
            }

            app.Run();
        }
    }
}
