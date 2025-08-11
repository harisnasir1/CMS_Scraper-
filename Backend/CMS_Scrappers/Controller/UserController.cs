using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
[ApiController]
[Route("api/[controller]")]
public class UserController:ControllerBase
{
    private readonly IUserService _userService;
    private readonly JwtSettings _jwtSettings;

    public UserController(IUserService userService, JwtSettings jwtSettings)
    {
        _userService = userService;
        _jwtSettings = jwtSettings;
    }
 
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
       
        return Ok("Scraper is ready");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegistrationDto dto)
    {
     
      var result = await _userService.RegisterUserAsync(dto);
      if(!result) return BadRequest("User already exists");
      return Ok("Registration sucessful");
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(UserloginDto dto)
    {
    
        var user=await _userService.AuthenticateUserAsync(dto.Email,dto.Password);
        if(user==null)return Unauthorized("Invalid credentials");

        var Token=GenerateJWtToken(user);
        Response.Cookies.Append("token", Token, new CookieOptions
        {
        HttpOnly = false,
        Secure = HttpContext.Request.IsHttps || string.Equals(HttpContext.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase),
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(new { message = "Login successful" });
    }
    
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if(userId==null)return BadRequest("Invalid token");
        var userinfo=_userService.Userinfo(userId); 
        return Ok(new { userinfo });
    }


    private string GenerateJWtToken(User user)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("name", user.Name)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}