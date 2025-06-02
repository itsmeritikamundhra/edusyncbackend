namespace EduSyncProject.DTO
{
    public class ResultReadDTO
    {
        public string Id { get; set; } = null!;

        public int Score { get; set; }

        public DateTime AttemptDate { get; set; }

        public string AssessmentId { get; set; } = null!;

        public string AssessmentTitle { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public int MaxScore { get; set; }
    }

    public class ResultCreateDTO
    {
        public int Score { get; set; }

        public DateTime AttemptDate { get; set; }

        public Guid AssessmentId { get; set; }

        public Guid UserId { get; set; }
    }

    public class ResultUpdateDTO
    {
        public int Score { get; set; }

        public DateTime AttemptDate { get; set; }
    }
}
