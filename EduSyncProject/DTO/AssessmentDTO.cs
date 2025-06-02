namespace EduSyncProject.DTO
{
    public class AssessmentReadDTO
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string Questions { get; set; } = null!;

        public int MaxScore { get; set; }

        public string CourseId { get; set; } = null!;

        public string CourseTitle { get; set; } = null!;
    }

    public class AssessmentCreateDTO
    {
        public string Title { get; set; } = null!;

        public string Questions { get; set; } = null!;

        public int MaxScore { get; set; }

        public Guid CourseId { get; set; }
    }

    public class AssessmentUpdateDTO
    {
        public string Title { get; set; } = null!;

        public string Questions { get; set; } = null!;

        public int MaxScore { get; set; }
    }
}
