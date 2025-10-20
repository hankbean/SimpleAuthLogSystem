namespace SimpleAuthLog.DTOs
{
    public class UserDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class UserUpdateDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
