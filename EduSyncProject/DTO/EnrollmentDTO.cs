namespace EduSyncProject.DTO
{
    /// <summary>
    /// DTO for student operations (enrollment/unenrollment)
    /// </summary>
    [System.ComponentModel.DisplayName("Student Enrollment")]
    public class StudentEnrollmentDTO
    {
        /// <summary>
        /// The ID of the course to enroll in or unenroll from
        /// </summary>
        public Guid CourseId { get; set; }
    }

    /// <summary>
    /// DTO for instructor operations (enrolling/unenrolling students)
    /// </summary>
    [System.ComponentModel.DisplayName("Instructor Student Enrollment")]
    public class InstructorEnrollmentDTO
    {
        /// <summary>
        /// The ID of the course
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// The ID of the student to enroll/unenroll
        /// </summary>
        public Guid StudentId { get; set; }
    }

    public class EnrollmentReadDTO
    {
        public string CourseId { get; set; } = null!;
        public string CourseTitle { get; set; } = null!;
        public string InstructorId { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public string? Description { get; set; }
        public string? MediaUrl { get; set; }
    }
} 