-- First verify the current state
SELECT c.CourseId, c.Title, c.InstructorId, u.Name, u.Role 
FROM dbo.Course c
JOIN dbo.User u ON c.InstructorId = u.UserId
WHERE c.CourseId = '7f4b81a4-a231-4f92-b4fb-56346e10c0b6';

-- Begin transaction
BEGIN TRANSACTION;

-- Update the instructor ID to the correct instructor
UPDATE dbo.Course
SET InstructorId = '2b6da9f5-20e6-4f1b-9a7b-847a307a2f38'  -- Your instructor ID
WHERE CourseId = '7f4b81a4-a231-4f92-b4fb-56346e10c0b6';   -- Cloud course ID

-- Verify the change
SELECT c.CourseId, c.Title, c.InstructorId, u.Name, u.Role 
FROM dbo.Course c
JOIN dbo.User u ON c.InstructorId = u.UserId
WHERE c.CourseId = '7f4b81a4-a231-4f92-b4fb-56346e10c0b6';

-- If everything looks correct, commit the transaction
COMMIT TRANSACTION; 