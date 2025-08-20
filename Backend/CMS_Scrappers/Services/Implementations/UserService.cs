using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using  Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

public class UserService:IUserService
{
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository repo)
    {
        _userRepository=repo;
    }

    public async Task<bool> RegisterUserAsync(UserRegistrationDto dto){
       var existinguser=await _userRepository.GetByEmailAsync(dto.Email);
       if (existinguser != null) return false;

       var hashedpassword = HashPassword(dto.password.Trim());

       var user=new User{
        Id=Guid.NewGuid(),
        Name = dto.Name.Trim(),
        Email =dto.Email.Trim(),
        Password = hashedpassword,
        CreatedAt =DateTime.UtcNow,
        UpdatedAt =DateTime.UtcNow
       };
       await _userRepository.AddUserAsync(user);
       return true;
    }

    public async Task<User> AuthenticateUserAsync(string email, string password)
    {
        var user= await _userRepository.GetByEmailAsync(email);
        if(user==null) return null;

        if(VerifyPassword(password,user.Password))
        {
            return user;
        }
        return null;
    }

    public string HashPassword(string password)
    {
        byte[] salt=new byte[128/8];
        using (var rng=RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
        return $"{Convert.ToBase64String(salt)}:{hashed}";
    }

    public async Task<UserinfoDto> Userinfo(string id)
    {
        return await _userRepository.Getuserinfo(id);
    }



    private bool VerifyPassword(string enteredPassword, string storedPassword)
    {
        var parts=storedPassword.Split(':');
        if(parts.Length!=2)return false;
        var salt=Convert.FromBase64String(parts[0]);
        var storedHash=parts[1];
        string enteredHash=Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password:enteredPassword.Trim(),
            salt:salt,
            prf:KeyDerivationPrf.HMACSHA256,
            iterationCount:10000,
            numBytesRequested:256/8
        ));
        return enteredHash==storedHash;
    }

  



}