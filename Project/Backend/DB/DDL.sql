create database findly_db;
go

use findly_db;
go

if object_id('dbo.Claims', 'u') is not null drop table dbo.Claims;
if object_id('dbo.Reports', 'u') is not null drop table dbo.Reports;
if object_id('dbo.Locations', 'u') is not null drop table dbo.Locations;
if object_id('dbo.Categories', 'u') is not null drop table dbo.Categories;
if object_id('dbo.Users', 'u') is not null drop table dbo.Users;
go

create table dbo.Users
(
    UserID int identity(1, 1) not null,
    Username nvarchar(100) not null,
    PasswordHash nvarchar(500) not null,
    Email nvarchar(150) not null,
    Phone nvarchar(30) null,
    Role int not null default 2,
    CreatedDate datetime not null default getdate(),

    constraint pk_users primary key clustered (UserID asc),
    constraint uq_users_username unique (Username),
    constraint uq_users_email unique (Email)
);
go

create table dbo.Categories
(
    CategoryID int identity(1, 1) not null,
    Name nvarchar(120) not null,

    constraint pk_categories primary key clustered (CategoryID asc),
    constraint uq_categories_name unique (Name)
);
go

create table dbo.Locations
(
    LocationID int identity(1, 1) not null,
    Name nvarchar(150) not null,

    constraint pk_locations primary key clustered (LocationID asc),
    constraint uq_locations_name unique (Name)
);
go

create table dbo.Reports
(
    ReportID int identity(1, 1) not null,
    Title nvarchar(200) not null,
    Description nvarchar(max) not null,
    ReportDate datetime not null,
    Type int not null,
    Status int not null default 1,
    Photo varbinary(max) null,
    PhotoContentType nvarchar(100) null,
    UserID int not null,
    CategoryID int not null,
    LocationID int not null,
    CreatedDate datetime not null default getdate(),

    constraint pk_reports primary key clustered (ReportID asc),
    constraint fk_reports_users foreign key (UserID) references dbo.Users (UserID),
    constraint fk_reports_categories foreign key (CategoryID) references dbo.Categories (CategoryID),
    constraint fk_reports_locations foreign key (LocationID) references dbo.Locations (LocationID)
);
go

create table dbo.Claims
(
    ClaimID int identity(1, 1) not null,
    CreatedDate datetime not null default getdate(),
    Status int not null default 1,
    Proof nvarchar(max) not null,
    PhotoPath nvarchar(500) null,
    UserID int not null,
    ReportID int not null,
    AdminID int null,
    RejectionReason nvarchar(1000) null,

    constraint pk_claims primary key clustered (ClaimID asc),
    constraint fk_claims_users foreign key (UserID) references dbo.Users (UserID),
    constraint fk_claims_reports foreign key (ReportID) references dbo.Reports (ReportID),
    constraint fk_claims_admins foreign key (AdminID) references dbo.Users (UserID)
);
go

create index ix_reports_userid on dbo.Reports (UserID);
create index ix_reports_categoryid on dbo.Reports (CategoryID);
create index ix_reports_locationid on dbo.Reports (LocationID);
create index ix_reports_status_type on dbo.Reports (Status, Type);
create index ix_claims_reportid on dbo.Claims (ReportID);
create index ix_claims_userid on dbo.Claims (UserID);
create index ix_claims_status on dbo.Claims (Status);
go

-- column descriptions (ms_description)
exec sys.sp_addextendedproperty
    @name = 'MS_Description',
    @value = 'admin = 1, user = 2',
    @level0type = 'SCHEMA', @level0name = 'dbo',
    @level1type = 'TABLE',  @level1name = 'Users',
    @level2type = 'COLUMN', @level2name = 'Role';
go

exec sys.sp_addextendedproperty
    @name = 'MS_Description',
    @value = 'lost = 1, found = 2',
    @level0type = 'SCHEMA', @level0name = 'dbo',
    @level1type = 'TABLE',  @level1name = 'Reports',
    @level2type = 'COLUMN', @level2name = 'Type';
go

exec sys.sp_addextendedproperty
    @name = 'MS_Description',
    @value = 'pending = 1, matched/resolved = 2, closed = 3',
    @level0type = 'SCHEMA', @level0name = 'dbo',
    @level1type = 'TABLE',  @level1name = 'Reports',
    @level2type = 'COLUMN', @level2name = 'Status';
go

exec sys.sp_addextendedproperty
    @name = 'MS_Description',
    @value = 'pending = 1, approved = 2, rejected = 3, cancelled = 4',
    @level0type = 'SCHEMA', @level0name = 'dbo',
    @level1type = 'TABLE',  @level1name = 'Claims',
    @level2type = 'COLUMN', @level2name = 'Status';
go