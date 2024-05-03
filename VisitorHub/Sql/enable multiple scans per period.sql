ALTER TABLE inoutrooms
  ADD allow_multiple_scans bit;
GO

update inoutrooms set allow_multiple_scans = 0;