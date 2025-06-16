using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using Weixin_Project.Entities;

namespace Weixin_Project.Utils
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<Product> Products { get; set; } // 商品
        public DbSet<Order> Orders { get; set; } // 订单
        public DbSet<OrderItem> OrderItems { get; set; } // 订单项
        public DbSet<Address> Addresses { get; set; } // 收货地址
        public DbSet<Cart> Carts { get; set; }           // 用户购物车
        public DbSet<CartItem> CartItems { get; set; }   // 购物车中的商品项
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        /// <summary>
        /// 模型创建时的配置
        /// </summary>
        /// <param name="modelBuilder">模型构建器</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // 必须调用基类的方法，以确保Identity相关的表被正确配置

            // 从当前程序集加载所有实现了IEntityTypeConfiguration<>接口的配置类
            // 您已有的ProductConfig就是通过这种方式加载的
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            // 为 Product 实体配置全局查询过滤器，实现软删除 (查询时自动排除IsDeleted=true的数据)
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            // 为 Address 实体配置全局查询过滤器，实现软删除
            modelBuilder.Entity<Address>().HasQueryFilter(a => !a.IsDeleted);

            // --- 开始配置 Product 实体 ---
            modelBuilder.Entity<Product>(entity => // 使用 lambda 表达式配置 Product 实体
            {
                entity.HasKey(p => p.Id);     // 指定主键

                entity.Property(p => p.Name)
                      .IsRequired()             // 设置为必需字段
                      .HasMaxLength(100);      // 设置最大长度为100

                // 根据您Product实体的定义，可以添加更多配置
                entity.Property(p => p.Description)
                      .HasMaxLength(500) // 假设最大长度500，如果实体中有定义则对应
                      .IsRequired(false); // 假设Description可以为空，如果默认为"无"，则不需要IsRequired()

                entity.Property(p => p.Price)
                      .HasColumnType("decimal(18,2)") // 明确指定数据库中的列类型，与实体注解一致
                      .IsRequired();

                entity.Property(p => p.Stock)
                      .IsRequired();

                entity.Property(p => p.ImageUrl)
                      .HasMaxLength(255) // 假设URL最大长度255
                      .IsRequired(false); // ImageUrl可以为空

                entity.Property(p => p.IsDeleted)
                      .IsRequired()
                      .HasDefaultValue(false); // 明确指定软删除标记的默认值为false

                entity.Property(p => p.CreateTime)
                      .IsRequired()
                      .HasDefaultValueSql("GETUTCDATE()"); // 或者 HasDefaultValue(DateTime.UtcNow) 如果数据库支持
                                                           // 对于SQL Server, GETUTCDATE() 是推荐的获取UTC时间的函数
            });
            // --- 结束 Product 实体配置 ---

            // 配置 Order 和 OrderItem 之间的一对多关系
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems) // 一个订单有多个订单项
                .WithOne(oi => oi.Order) // 一个订单项属于一个订单
                .HasForeignKey(oi => oi.OrderId) // 外键是OrderItem中的OrderId
                .OnDelete(DeleteBehavior.Cascade); // 当订单被删除时，级联删除其下的所有订单项

            // 配置 Order 和 IdentityUser 之间的一对多关系 (一个用户可以有多个订单)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User) // 一个订单属于一个用户
                .WithMany() // IdentityUser默认可能没有直接的Orders导航属性，所以这里不指定
                .HasForeignKey(o => o.UserId) // 外键是Order中的UserId
                .OnDelete(DeleteBehavior.Restrict); // 当用户被删除时，限制删除，除非其所有订单也处理完毕（或者根据业务调整为Cascade或SetNull）

            // 配置 Order 和 Address 之间的一对多关系 (一个地址可以被多个订单使用，但一个订单只有一个收货地址)
            // 注意：这里的设计是一个订单关联一个特定的地址记录。
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress) // 一个订单有一个收货地址
                .WithMany() // Address实体通常不直接导航回Orders（避免循环引用和复杂性）
                .HasForeignKey(o => o.ShippingAddressId) // 外键是Order中的ShippingAddressId
                .OnDelete(DeleteBehavior.Restrict); // 当地址被删除时，限制删除，如果该地址仍被订单使用。
                                                    // 考虑：如果地址可以被编辑，订单中保存的地址信息应该是下单时的快照，或者地址实体不应该被真正删除，而是标记为不可用。
                                                    // 简单起见，这里用Restrict。

            // 配置 OrderItem 和 Product 之间的一对多关系 (一个商品可以出现在多个订单项中)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product) // 一个订单项对应一个商品
                .WithMany() // Product实体通常不直接导航回OrderItems
                .HasForeignKey(oi => oi.ProductId) // 外键是OrderItem中的ProductId
                .OnDelete(DeleteBehavior.Restrict); // 当商品被删除（或软删除）时，限制删除相关的订单项，通常订单项应保留历史销售记录。

            // 配置 Address 和 IdentityUser 之间的一对多关系 (一个用户可以有多个收货地址)
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User) // 一个地址属于一个用户
                .WithMany() // IdentityUser默认没有Addresses导航属性
                .HasForeignKey(a => a.UserId) // 外键是Address中的UserId
                .OnDelete(DeleteBehavior.Cascade); // 当用户被删除时，级联删除其所有收货地址

            // --- 购物车相关的Fluent API配置 ---
            // Cart 与 IdentityUser (一对一关系，一个用户一个购物车)
            // IdentityUser 默认没有 Cart 导航属性，所以从 Cart 端配置
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne() // 如果 IdentityUser 有 Cart 导航属性，这里是 WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId) // 外键在 Cart 表的 UserId
                .OnDelete(DeleteBehavior.Cascade); // 用户删除时，级联删除其购物车

            // CartItem 与 Cart (一对多关系)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart) // 一个购物车项属于一个购物车
                .WithMany(c => c.Items) // 一个购物车有多个项
                .HasForeignKey(ci => ci.CartId) // 外键是CartItem中的CartId
                .OnDelete(DeleteBehavior.Cascade); // 删除购物车时，级联删除其所有项

            // CartItem 与 Product (多对一关系)
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product) // 一个购物车项对应一个商品
                .WithMany() // Product实体通常不直接导航回CartItems
                .HasForeignKey(ci => ci.ProductId) // 外键是CartItem中的ProductId
                .OnDelete(DeleteBehavior.Restrict); // 当商品被删除（或软删除）时，限制删除购物车中的项。
                                                    // 业务上可能需要提示用户商品已下架并从购物车移除。
                                                    // 如果商品被硬删除，这里会导致问题，所以软删除更好。

            // 确保用户的购物车是唯一的 (UserId应该是唯一的)
            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();
            // --- 结束购物车配置 ---
        }
    }
}
