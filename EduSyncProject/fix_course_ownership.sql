-- Begin transaction
BEGIN TRANSACTION;

-- Update Java course ownership to your instructor ID
UPDATE Course
SET InstructorId = '9e03a635-54b0-43be-aa6d-116028fb6656'  -- Your instructor ID
WHERE CourseId = '53401d4e-af0b-4ce3-b7b8-e22be3aa9af3';   -- Java course ID

-- Verify the changes
SELECT c.CourseId, c.Title, c.InstructorId, u.Name, u.Role 
FROM Course c
JOIN [User] u ON c.InstructorId = u.UserId
WHERE c.Title IN ('Java', 'Python');

-- If everything looks correct, commit the transaction
COMMIT TRANSACTION; 