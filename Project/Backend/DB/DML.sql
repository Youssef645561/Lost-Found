use findly_db;

set nocount on;

-- 1. seed users (Role: admin = 1, user = 2)
if not exists (select 1 from dbo.Users where Username = 'admin')
begin
    insert into dbo.Users (Username, PasswordHash, Email, Phone, Role)
    values ('admin', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'admin@findly.edu.eg', '01000000000', 1);
end;

if not exists (select 1 from dbo.Users where Username = 'youssef')
begin
    insert into dbo.Users (Username, PasswordHash, Email, Phone, Role)
    values ('youssef', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'youssef@findly.edu.eg', '01011111111', 2);
end;

if not exists (select 1 from dbo.Users where Username = 'sara_ahmed')
begin
    insert into dbo.Users (Username, PasswordHash, Email, Phone, Role)
    values ('sara_ahmed', '03ac674216f3e15c761ee1a5e255f067953623c8b388b4459e13f978d7c846f4', 'sara.ahmed@findly.edu.eg', '01022222222', 2);
end;
go

-- 2. seed categories
if not exists (select 1 from dbo.Categories where Name = 'Electronics')
begin
    insert into dbo.Categories (Name)
    values ('Electronics');
end;

if not exists (select 1 from dbo.Categories where Name = 'IDs & Cards')
begin
    insert into dbo.Categories (Name)
    values ('IDs & Cards');
end;

if not exists (select 1 from dbo.Categories where Name = 'Bags & Books')
begin
    insert into dbo.Categories (Name)
    values ('Bags & Books');
end;
go

-- 3. seed locations
if not exists (select 1 from dbo.Locations where Name = 'Main Library')
begin
    insert into dbo.Locations (Name)
    values ('Main Library');
end;

if not exists (select 1 from dbo.Locations where Name = 'CS Building')
begin
    insert into dbo.Locations (Name)
    values ('CS Building');
end;
go

-- 4. seed reports (Type: lost = 1, found = 2 | Status: pending = 1, matched/resolved = 2, closed = 3)
declare @youssef_id int = (select top 1 UserID from dbo.Users where Username = 'youssef');
declare @sara_id int = (select top 1 UserID from dbo.Users where Username = 'sara_ahmed');

declare @cat_electronics int = (select top 1 CategoryID from dbo.Categories where Name = 'Electronics');
declare @cat_ids int = (select top 1 CategoryID from dbo.Categories where Name = 'IDs & Cards');

declare @loc_library int = (select top 1 LocationID from dbo.Locations where Name = 'Main Library');
declare @loc_cs int = (select top 1 LocationID from dbo.Locations where Name = 'CS Building');

if @youssef_id is not null and @cat_electronics is not null and @loc_cs is not null
   and not exists (select 1 from dbo.Reports where Title = 'Black wireless earbuds near Lab 4')
begin
    insert into dbo.Reports
        (Title, Description, ReportDate, Type, Status, UserID, CategoryID, LocationID)
    values
        ('Black wireless earbuds near Lab 4',
         'Lost a black charging case with earbuds on the back desk after afternoon lecture.',
         dateadd(day, -3, getdate()), 1, 1, @youssef_id, @cat_electronics, @loc_cs);
end;

if @sara_id is not null and @cat_ids is not null and @loc_library is not null
   and not exists (select 1 from dbo.Reports where Title = 'Found university ID card on reading table')
begin
    insert into dbo.Reports
        (Title, Description, ReportDate, Type, Status, UserID, CategoryID, LocationID)
    values
        ('Found university ID card on reading table',
         'Found a student ID card left near the quiet study area.',
         dateadd(day, -2, getdate()), 2, 1, @sara_id, @cat_ids, @loc_library);
end;
go

-- 5. seed claims (Status: pending = 1, approved = 2, rejected = 3, cancelled = 4)
declare @found_report_id int = (select top 1 ReportID from dbo.Reports where Title = 'Found university ID card on reading table');
declare @youssef_claimant_id int = (select top 1 UserID from dbo.Users where Username = 'youssef');

if @found_report_id is not null and @youssef_claimant_id is not null
   and not exists (select 1 from dbo.Claims where ReportID = @found_report_id and UserID = @youssef_claimant_id)
begin
    insert into dbo.Claims
        (Status, Proof, PhotoPath, UserID, ReportID, AdminID, RejectionReason)
    values
        (1,
         'I can confirm the last digits of the ID number and department name printed on the card.',
         null, @youssef_claimant_id, @found_report_id, null, null);
end;
go