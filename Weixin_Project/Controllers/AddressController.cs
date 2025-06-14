using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Weixin_Project.DTOs; // 引入DTOs命名空间
using Weixin_Project.Entities; // 引入Entities命名空间

namespace Weixin_Project.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // 需要用户登录
    public class AddressController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager; // 虽然不直接用，但保留以备将来扩展

        public AddressController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // 获取当前用户的所有收货地址列表
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressViewModel>>> GetMyAddresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            // 查询时排除软删除的地址，并按是否默认、ID排序
            var addresses = await _dbContext.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted) // !a.IsDeleted 会被全局查询过滤器处理，但显式写出更清晰
                .OrderByDescending(a => a.IsDefault) // 默认地址优先
                .ThenByDescending(a => a.Id)        // 然后按ID降序（新的在前）
                .Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    ContactName = a.ContactName,
                    PhoneNumber = a.PhoneNumber,
                    Province = a.Province,
                    City = a.City,
                    StreetAddress = a.StreetAddress,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault
                    // FullAddress 通过DTO的get属性自动生成
                })
                .ToListAsync();

            return Ok(addresses);
        }

        // 获取当前用户的单个收货地址详情
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressViewModel>> GetAddressById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var address = await _dbContext.Addresses
                .Where(a => a.Id == id && a.UserId == userId && !a.IsDeleted)
                .Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    ContactName = a.ContactName,
                    PhoneNumber = a.PhoneNumber,
                    Province = a.Province,
                    City = a.City,
                    StreetAddress = a.StreetAddress,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault
                })
                .FirstOrDefaultAsync();

            if (address == null)
            {
                return NotFound(new { message = "地址不存在或您无权查看。" });
            }

            return Ok(address);
        }

        // 用户创建新的收货地址
        [HttpPost]
        public async Task<ActionResult<AddressViewModel>> CreateAddress([FromBody] CreateAddressRequestDto createAddressDto)
        {
            // FluentValidation 会自动验证 createAddressDto

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            // 如果将新地址设为默认，则需要将该用户其他地址的IsDefault设为false
            if (createAddressDto.IsDefault)
            {
                var existingDefaultAddresses = await _dbContext.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                    .ToListAsync();
                foreach (var addr in existingDefaultAddresses)
                {
                    addr.IsDefault = false;
                    _dbContext.Addresses.Update(addr);
                }
            }

            var newAddress = new Address
            {
                UserId = userId,
                ContactName = createAddressDto.ContactName,
                PhoneNumber = createAddressDto.PhoneNumber,
                Province = createAddressDto.Province,
                City = createAddressDto.City,
                StreetAddress = createAddressDto.StreetAddress,
                PostalCode = createAddressDto.PostalCode,
                IsDefault = createAddressDto.IsDefault,
                IsDeleted = false // 新建地址不是软删除状态
            };

            _dbContext.Addresses.Add(newAddress);
            await _dbContext.SaveChangesAsync();

            var addressViewModel = new AddressViewModel
            {
                Id = newAddress.Id,
                ContactName = newAddress.ContactName,
                PhoneNumber = newAddress.PhoneNumber,
                Province = newAddress.Province,
                City = newAddress.City,
                StreetAddress = newAddress.StreetAddress,
                PostalCode = newAddress.PostalCode,
                IsDefault = newAddress.IsDefault
            };
            // 返回 201 Created，并包含新创建的地址信息和获取该地址的URI
            return CreatedAtAction(nameof(GetAddressById), new { id = newAddress.Id }, addressViewModel);
        }

        // 用户更新现有的收货地址
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateAddressRequestDto updateAddressDto)
        {
            // FluentValidation 会自动验证 updateAddressDto

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var addressToUpdate = await _dbContext.Addresses
                                        .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted);

            if (addressToUpdate == null)
            {
                return NotFound(new { message = "要更新的地址不存在或您无权操作。" });
            }

            // 如果将此地址设为默认，且它之前不是默认的，则需要将其他地址的IsDefault设为false
            if (updateAddressDto.IsDefault && !addressToUpdate.IsDefault)
            {
                var existingDefaultAddresses = await _dbContext.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != id && !a.IsDeleted)
                    .ToListAsync();
                foreach (var addr in existingDefaultAddresses)
                {
                    addr.IsDefault = false;
                    _dbContext.Addresses.Update(addr);
                }
            }
            // 如果一个已经是默认的地址被更新为非默认，而用户没有其他默认地址，需要考虑业务逻辑
            // (例如，是否强制至少有一个默认地址，或者允许没有默认地址)
            // 当前逻辑：如果用户将唯一的默认地址改为非默认，则该用户将没有默认地址。

            addressToUpdate.ContactName = updateAddressDto.ContactName;
            addressToUpdate.PhoneNumber = updateAddressDto.PhoneNumber;
            addressToUpdate.Province = updateAddressDto.Province;
            addressToUpdate.City = updateAddressDto.City;
            addressToUpdate.StreetAddress = updateAddressDto.StreetAddress;
            addressToUpdate.PostalCode = updateAddressDto.PostalCode;
            addressToUpdate.IsDefault = updateAddressDto.IsDefault;

            _dbContext.Addresses.Update(addressToUpdate);
            await _dbContext.SaveChangesAsync();

            return NoContent(); // HTTP 204 No Content 表示更新成功，响应体中无内容
            // 或者 return Ok(new { message = "地址更新成功" });
        }

        // 用户软删除自己的收货地址
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var addressToDelete = await _dbContext.Addresses
                                        .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted);

            if (addressToDelete == null)
            {
                return NotFound(new { message = "要删除的地址不存在或您无权操作。" });
            }

            addressToDelete.IsDeleted = true;
            addressToDelete.IsDefault = false; // 删除的地址不能是默认地址
            _dbContext.Addresses.Update(addressToDelete);
            await _dbContext.SaveChangesAsync();

            return NoContent(); // HTTP 204 No Content
            // 或者 return Ok(new { message = "地址删除成功" });
        }

        // (可选) 用户设置某个地址为默认地址
        [HttpPut("{id}/SetDefault")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "无法获取用户信息。" });
            }

            var addressToSetDefault = await _dbContext.Addresses
                                            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted);
            if (addressToSetDefault == null)
            {
                return NotFound(new { message = "地址不存在或您无权操作。" });
            }

            if (addressToSetDefault.IsDefault)
            {
                return Ok(new { message = "该地址已经是默认地址。" }); // 或者返回 204
            }

            // 先将用户其他所有地址设为非默认
            var currentDefaultAddresses = await _dbContext.Addresses
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .ToListAsync();

            foreach (var addr in currentDefaultAddresses)
            {
                addr.IsDefault = false;
                _dbContext.Addresses.Update(addr);
            }

            // 再将指定地址设为默认
            addressToSetDefault.IsDefault = true;
            _dbContext.Addresses.Update(addressToSetDefault);

            await _dbContext.SaveChangesAsync();
            return Ok(new { message = "默认地址设置成功。" });
        }
    }
}