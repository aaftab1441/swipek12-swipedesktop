ALTER TABLE CorrectiveActions
  ADD Operator varchar(20);
GO

update CorrectiveActions set Operator = 'Equal';