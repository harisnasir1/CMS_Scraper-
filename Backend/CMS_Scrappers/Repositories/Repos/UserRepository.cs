using Microsoft.EntityFrameworkCore;

public class UserRepository:IUserRepository{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context)
    {
        _context=context;
    }
    public async Task<User>GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u=>u.Email== email);
    }
    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    public async Task <UserinfoDto> Getuserinfo(string id)
    {
       if (!Guid.TryParse(id, out var guidId))
        return null; 

       var user = await _context.Users
        .Where(u => u.Id == guidId)
        .Select(u => new UserinfoDto
        {
            Id = u.Id.ToString(),
            Name = u.Name,
            Email = u.Email,
            Role = "Admin"
        })
        .FirstOrDefaultAsync();

      return user;
    }
}