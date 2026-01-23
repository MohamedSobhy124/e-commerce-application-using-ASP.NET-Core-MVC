-- SQL Script to fix existing users after changing from IdentityUser to ApplicationUser
-- Run this script on your database to update existing users

-- Update all users that have NULL or 'IdentityUser' discriminator to 'ApplicationUser'
-- This ensures they can be read correctly by Entity Framework
UPDATE AspNetUsers
SET Discriminator = 'ApplicationUser'
WHERE Discriminator IS NULL OR Discriminator = 'IdentityUser';

-- Ensure Name column has a default value for users that might have NULL
-- (This is optional but recommended to prevent validation issues)
UPDATE AspNetUsers
SET Name = COALESCE(Name, UserName, Email)
WHERE Name IS NULL AND Discriminator = 'ApplicationUser';
