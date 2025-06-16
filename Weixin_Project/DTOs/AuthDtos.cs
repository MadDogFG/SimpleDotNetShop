namespace Weixin_Project.DTOs
{
    public class LoginRequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Password2 { get; set; }
    }
}
