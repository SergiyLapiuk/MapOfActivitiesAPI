using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MapOfActivitiesAPI.Models;
using MapOfActivitiesAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace MapOfActivitiesAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly MapOfActivitiesAPIContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, IConfiguration configuration, RoleManager<IdentityRole> roleManager, ITokenService tokenService, MapOfActivitiesAPIContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _tokenService = tokenService;
            _roleManager = roleManager;
            _context = context;
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> UserLogin([FromBody] LoginModel request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                if (!await _userManager.IsEmailConfirmedAsync(user)&& !await _userManager.IsInRoleAsync(user, ApplicationUserRoles.Admin))
                {
                    return BadRequest("Email not confirmed");
                }

                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim("email", user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim("roles", role));
                };

                var token = _tokenService.CreateToken(authClaims);
                var refreshToken = _tokenService.GenerateRefreshToken();

                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenValidityInDays);

                await _userManager.UpdateAsync(user);

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo,
                    Roles = userRoles,
                    UserId = user.Id
                });
            }
            return BadRequest("Invalid email or password");
        }

        [HttpPost]
        [Route("userid-from-token")]
        public async Task<ActionResult<User>> UserIdFromToken(TokenModel tokenModel)
        {
            if (tokenModel is null)
                return BadRequest("Invalid client request");

            string accessToken = tokenModel.AccessToken;
            string refreshToken = tokenModel.RefreshToken;

            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);


            string email = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            string role = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Invalid email in the token");

            var _user = await _context.Users.FirstOrDefaultAsync(c => c.Email == email);

            var userData = new
            {
                user = _user,
                Roles = role
            };

            return Ok(userData);

        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User already exists!" });

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email
            };

            User userProfile = new()
            {
                UserId = user.Id,
                Name = model.Name,
                Email = model.Email
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User creation failed! Please check user details and try again." });
            else
            {
                _context.Users.Add(userProfile);
                await _context.SaveChangesAsync();

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = $"http://localhost:9000/#/start-menu?userId={user.Id}&code={code}";
                EmailService emailService = new EmailService();
                await emailService.SendEmailAsync(model.Email, "Confirm your account", "Ваш обліковий запис майже готовий!", callbackUrl);
            }

            return Ok(new ResponseModel { Status = "Success", Message = "User created successfully!" });
        }


        [HttpPost]
        [Route("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByEmailAsync(model.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User already exists!" });

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Email
            };

            User userProfile = new()
            {
                UserId = user.Id,
                Name = model.Name,
                Email = model.Email
            };
            var result = await _userManager.CreateAsync(user, model.Password);
         

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.Admin));

            if (await _roleManager.RoleExistsAsync(ApplicationUserRoles.Admin))
            {
                await _userManager.AddToRoleAsync(user, ApplicationUserRoles.Admin);
            }

            _context.Users.Add(userProfile);
            await _context.SaveChangesAsync();

            return Ok(new ResponseModel { Status = "Success", Message = "User created successfully!" });
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "UserId or code = null!" });
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User = null!" });
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync(ApplicationUserRoles.User))
                    await _roleManager.CreateAsync(new IdentityRole(ApplicationUserRoles.User));

                if (await _roleManager.RoleExistsAsync(ApplicationUserRoles.User))
                {
                    await _userManager.AddToRoleAsync(user, ApplicationUserRoles.User);
                }

                return Ok();
            }
            else
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return BadRequest("User not found.");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = $"http://localhost:9000/#/reset-password?userId={user.Id}&code={code}";
            EmailService emailService = new EmailService();
            await emailService.SendEmailAsync(user.Email, "Reset Password", "Ваш запит на скидання паролю успішно оброблено!", callbackUrl);

            return Ok("You may now reset your password.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel request)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null)
            {
                return BadRequest("User not found.");
            }
            var result = await _userManager.ResetPasswordAsync(user, request.Code, request.Password);
            if (result.Succeeded)
            {
                return Ok("Password successfully reset.");
            }
            foreach (var error in result.Errors)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = error.Description });
            }
            return Ok("Password successfully reset.");
        }
    }
}