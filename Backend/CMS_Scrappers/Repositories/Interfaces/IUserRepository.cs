public interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task AddUserAsync(User user);

    Task <UserinfoDto> Getuserinfo(string id);
}
