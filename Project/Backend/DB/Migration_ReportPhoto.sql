-- Run once on an EXISTING findly_db (no need if you recreate the DB from DDL.sql).
use findly_db;
go
if col_length('dbo.Reports', 'Photo') is null
    alter table dbo.Reports add Photo varbinary(max) null, PhotoContentType nvarchar(100) null;
go
if col_length('dbo.Reports', 'PhotoPath') is not null
    alter table dbo.Reports drop column PhotoPath;
go
