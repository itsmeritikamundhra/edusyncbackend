namespace EduSyncProject.DTO
{
    public class CourseReadDTO
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string InstructorId { get; set; } = null!;

        public string InstructorName { get; set; } = null!;

        public string? MediaUrl { get; set; }
    }

    public class CourseCreateDTO
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? MediaUrl { get; set; }
    }

    public class CourseUpdateDTO
    {
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public string? MediaUrl { get; set; }
    }
}
