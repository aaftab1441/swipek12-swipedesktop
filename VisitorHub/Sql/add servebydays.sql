alter table correctiveactions
add ServeByDays int
go
update CorrectiveActions set servebydays = 0 where servebydays is null
go