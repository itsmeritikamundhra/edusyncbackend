namespace EduSyncProject.DTO.Auths
{
    public class RegisterDto
    {
        public string Name { get; set; }
        public string Role { get; set; }  // "Student" or "Instructor"
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
