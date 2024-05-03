

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[InsertStudent]
	-- Add the parameters for the stored procedure here
	@SchoolId int,
	@PersonId int,
	@StudentId int,
	@IdNumber varchar(20),
	@FirstName varchar(25),
	@LastName varchar(25),
	@DateOfBirth DateTime = '1/1/1900',
	@Bus varchar(25) = null,
	@Grade varchar(2) = null,
	@Homeroom varchar(25) = null,
	@Gender varchar(1) = null,
	@MiddleName varchar(25) = null,
	@LunchCode varchar(50) = null
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	BEGIN TRANSACTION;

	INSERT INTO PERSON(PersonID, SchoolID, PersonTypeID, SSN, LASTNAME, FIRSTNAME, DOB, PhotoPath, LastUpdated, ACTIVE)
	VALUES(@PersonId, @SchoolId, 1, @IdNumber, @LastName, @FirstName, @DateOfBirth, @IdNumber + '.jpg', getdate(), 1)

	INSERT INTO STUDENTS(StudentID, SchoolID, PersonID, StudentNumber, Homeroom, Grade, GUID, LastUpdated, Bus)
	VALUES(@StudentId, @SchoolId, @PersonId, @IdNumber, @Homeroom, @Grade, NEWID(), getdate(), @Bus)

	COMMIT;

	select @PersonId, @StudentId
END