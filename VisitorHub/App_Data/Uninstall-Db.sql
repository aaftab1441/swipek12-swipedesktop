IF EXISTS(select * from master.sys.databases where name='SwipeDesktop')
alter database SwipeDesktop set single_user with rollback immediate;
DROP DATABASE SwipeDesktop;