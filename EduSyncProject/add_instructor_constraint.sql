-- Add constraint to ensure only instructors can be course instructors
ALTER TABLE dbo.Course
ADD CONSTRAINT CK_Course_InstructorRole
CHECK (
    InstructorId IN (
        SELECT UserId 
        FROM dbo.[User] 
        WHERE Role = 'Instructor'
    )
); 