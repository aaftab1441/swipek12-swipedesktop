
ALTER PROCEDURE [dbo].[InsertStaff]
	-- Add the parameters for the stored procedure here
	@SchoolId int,
	@PersonId int,
	@IdNumber varchar(20),
	@FirstName varchar(25),
	@LastName varchar(25),
	@DateOfBirth DateTime = '1/1/1900',
	@JobTitle varchar(25) = null,
	@OfficeLocation varchar(25) = null,
	@Gender varchar(1) = null,
	@MiddleName varchar(25) = null
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	BEGIN TRANSACTION;
	
	INSERT INTO PERSON(PersonID, SchoolID, PersonTypeID, SSN, LASTNAME, FIRSTNAME, DOB, PhotoPath, LASTUPDATED, ACTIVE)
	VALUES(@PersonId, @SchoolId, 8, @IdNumber, @LastName, @FirstName, @DateOfBirth, @IdNumber + '.jpg', getdate(), 1)

	INSERT INTO STAFF(PersonID, SchoolID, EmployeeNumber, Title, OfficeLocation)
	VALUES(@PersonId, @SchoolId, @IdNumber, @JobTitle, @OfficeLocation)

	
	COMMIT;
	select @PersonId
END