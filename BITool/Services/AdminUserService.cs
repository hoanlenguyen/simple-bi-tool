using BITool.DBContext;
using BITool.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BITool.Services
{
    public static class AdminUserService
    {
        public static void AddAdminUserService(this WebApplication app)
        {
            app.MapPost("auth/register", [AllowAnonymous] async (UserManager<AdminUser> userManager, AdminCreateOrUpdateDto input) =>
            {
                var user = new AdminUser
                {
                    UserName = input.UserName ?? input.Email,
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Email = input.Email
                };

                var result = await userManager.CreateAsync(user, input.Password);

                if (result.Succeeded)
                    return Results.Ok();

                return Results.BadRequest();
            });

            app.MapPost("auth/login", [AllowAnonymous] async (IConfiguration config, UserManager<AdminUser> userManager, AdminLoginDto input) =>
            {
                var user = await userManager.FindByNameAsync(input.UserName);
                if (user is null)
                    user = await userManager.FindByEmailAsync(input.UserName);

                if (user is null)
                    return Results.Unauthorized();

                if (await userManager.CheckPasswordAsync(user, input.Password))
                {
                    var issuer = config["Jwt:Issuer"];
                    var audience = config["Jwt:Audience"];
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                    var claims= new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email)
                    };
                    var token = new JwtSecurityToken(
                        issuer: issuer,
                        audience: audience,
                        signingCredentials: credentials,
                        claims: claims);
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var accessToken = tokenHandler.WriteToken(token);
                    return Results.Ok(new
                    {
                        accessToken = accessToken,
                        email = user.Email,
                        name = user.FirstName
                    });
                }
                else
                {
                    return Results.Unauthorized();
                }
            });
            app.MapPost("auth/checkConfig", [Authorize] async (IConfiguration config, IHttpContextAccessor httpContextAccessor) =>
            {
                return Results.Ok(new
                {
                    conn = config["ConnectionStrings:DefaultConnection"],
                    userId= httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    email= httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email)
                });
            });
        }
    }
}