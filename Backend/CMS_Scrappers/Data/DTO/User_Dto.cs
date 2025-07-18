using Microsoft.AspNetCore.Identity;

public class UserRegistrationDto
{
    public string Name {get;set;}="";
    public string Email {get;set;}="";
    public string password{get;set;}="";
}

public class UserloginDto
{
    public string Email {get;set;}="";
    public string Password {get;set;}="";
}

public class UserinfoDto
{
        public string Id {get;set;}="";
        public string Name {get;set;}="";
        public string Email {get;set;}="";
        public string Role{get;set;}="";
 
}