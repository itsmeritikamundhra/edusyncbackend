-- Create the StudentCourses table for many-to-many relationship
CREATE TABLE StudentCourses (
    StudentsUserId UNIQUEIDENTIFIER NOT NULL,
    EnrolledCoursesId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_StudentCourses PRIMARY KEY (StudentsUserId, EnrolledCoursesId),
    CONSTRAINT FK_StudentCourses_Course FOREIGN KEY (EnrolledCoursesId) 
        REFERENCES Course(CourseId) ON DELETE CASCADE,
    CONSTRAINT FK_StudentCourses_User FOREIGN KEY (StudentsUserId) 
        REFERENCES [User](UserId) ON DELETE CASCADE
);

-- Create index for better query performance
CREATE INDEX IX_StudentCourses_EnrolledCoursesId 
ON StudentCourses(EnrolledCoursesId); 