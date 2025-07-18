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

        var Token=GenerateJWtToken(dto.Email);
        Response.Cookies.Append("token", Token, new CookieOptions
        {
        HttpOnly = true,
        Secure = false, 
        SameSite = SameSiteMode.Lax, // or SameSiteMode.Strict/SameSiteMode.None (if on cross-domain with HTTPS)
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
  

    private string GenerateJWtToken(string email)
    {
      var claims=new[]
      {
        new Claim(JwtRegisteredClaimNames.Sub,email),
        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
      };
      var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
      var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
      var Token=new JwtSecurityToken(
        issuer:_jwtSettings.Issuer,
        audience:_jwtSettings.Audience,
        claims:claims,
        expires:DateTime.Now.AddMinutes(30),
        signingCredentials:creds
      );
      return new JwtSecurityTokenHandler().WriteToken(Token);
    }
}