using System;
using System.Collections.Generic;

namespace EduSyncProject.Models;

public partial class Course
{
    public Guid CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public Guid InstructorId { get; set; }

    public string? MediaUrl { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual User Instructor { get; set; } = null!;

    public virtual ICollection<User> Students { get; set; } = new List<User>();
}
