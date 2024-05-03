select * from studenttimetable

EXEC DataReplicatorConnector @Action='CREATE', @DataSource = '5.6.0.7';

insert into studenttimetable
exec DataReplicatorConnector.Swipek12.dbo.sp_GetStudentTimeTable '11102495'