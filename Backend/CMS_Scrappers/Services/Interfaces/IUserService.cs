public interface IUserService
{
    Task<bool> RegisterUserAsync(UserRegistrationDto dto);
    Task<User> AuthenticateUserAsync(string email, string password);
    Task <UserinfoDto> Userinfo(string id);
    
}
