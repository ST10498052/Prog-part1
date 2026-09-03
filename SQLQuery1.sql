/* ============================================================
   RaceDay Database Script
   System Planning and Database - Part 1
   Run this script on a clean SQL Server instance using SSMS.
   ============================================================ */

USE master;
GO

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* Defensive drops, in FK-safe order, in case the database above
   already existed and could not be dropped (e.g. this query window
   was still connected to it). Safe to run even on a brand-new DB. */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* ------------------------------------------------------------
   1. Roles
   Lookup table for the two system roles.
   ------------------------------------------------------------ */
CREATE TABLE Roles (
    RoleId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleName        VARCHAR(20)     NOT NULL UNIQUE
                        CONSTRAINT CK_Roles_Name CHECK (RoleName IN ('Organiser', 'Participant'))
);
GO

/* ------------------------------------------------------------
   2. Users
   Holds both Organisers and Participants, distinguished by RoleId.
   ------------------------------------------------------------ */
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT             NOT NULL,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    PhoneNumber     VARCHAR(20)     NULL,
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

/* ------------------------------------------------------------
   3. Events
   Owned by an Organiser (Users). Captures name, description,
   date, location, distance, and event type as required.
   ------------------------------------------------------------ */
CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId     INT             NOT NULL,
    Name            NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(MAX)   NULL,
    EventDate       DATETIME        NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    DistanceKm      DECIMAL(5,2)    NOT NULL,
    EventType       VARCHAR(10)     NOT NULL
                        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Draft'
                        CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft', 'Published', 'Cancelled', 'Completed')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId)
);
GO

/* ------------------------------------------------------------
   4. Categories
   Age-based or distance-based categories within an event
   (e.g. "Under 20", "Senior", "10km", "21km").
   ------------------------------------------------------------ */
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT             NOT NULL,
    Name            NVARCHAR(100)   NOT NULL,
    Description     NVARCHAR(255)   NULL,
    MinAge          INT             NULL,
    MaxAge          INT             NULL,
    DistanceKm      DECIMAL(5,2)    NULL,
    EntryFee        DECIMAL(8,2)    NOT NULL DEFAULT 0,
    MaxParticipants INT             NOT NULL,
    CONSTRAINT FK_Categories_Event FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

/* ------------------------------------------------------------
   5. Enrolments
   Records the link between a Participant, the Event, and the
   selected Category.
   ------------------------------------------------------------ */
CREATE TABLE Enrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId   INT             NOT NULL,
    EventId         INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    BibNumber       VARCHAR(10)     NOT NULL UNIQUE,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Confirmed', 'Cancelled', 'Withdrawn')),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId)
);
GO

/* ------------------------------------------------------------
   6. Results
   One result per Enrolment: finish time and finishing position.
   ------------------------------------------------------------ */
CREATE TABLE Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          VARCHAR(20)     NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DSQ')),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

-- Roles
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Users: 2 Organisers, 2 Participants
INSERT INTO Users (RoleId, FullName, Email, PasswordHash, PhoneNumber) VALUES
(1, 'Thandeka Mokoena', 'thandeka@raceday.co.za', 'HASHED_PASSWORD_1', '0721234567'),
(1, 'Pieter van der Merwe', 'pieter@raceday.co.za', 'HASHED_PASSWORD_2', '0837654321'),
(2, 'Lindiwe Nkosi', 'lindiwe.nkosi@example.com', 'HASHED_PASSWORD_3', '0611122233'),
(2, 'Sipho Dlamini', 'sipho.dlamini@example.com', 'HASHED_PASSWORD_4', '0729988776');
GO

-- Events: 3 events (Organiser 1 hosts one, Organiser 2 hosts two)
INSERT INTO Events (OrganiserId, Name, Description, EventDate, Location, DistanceKm, EventType, Status) VALUES
(1, 'Comrades Marathon 2027', 'The ultimate human race between Pietermaritzburg and Durban.', '2027-06-13 05:30:00', 'Pietermaritzburg, KwaZulu-Natal', 90.00, 'Run', 'Published'),
(2, 'Cape Town Cycle Tour 2027', 'The world''s largest individually timed cycle race.', '2027-03-08 06:00:00', 'Green Point, Cape Town', 109.00, 'Cycle', 'Published'),
(2, 'Soweto Marathon 2027', 'Community running festival through the streets of Soweto.', '2027-11-07 06:00:00', 'FNB Stadium, Soweto', 21.10, 'Run', 'Published');
GO

-- Categories: mix of age-based and distance-based, per event
INSERT INTO Categories (EventId, Name, Description, MinAge, MaxAge, DistanceKm, EntryFee, MaxParticipants) VALUES
(1, 'Senior', 'Open senior category, 20 years and older.', 20, NULL, 90.00, 950.00, 20000),
(2, 'Individual Entry', 'Open individual entry, all ages.', 15, NULL, 109.00, 650.00, 35000),
(3, '10km Fun Run', 'Shorter distance option for the festival.', 12, NULL, 10.00, 150.00, 5000),
(3, '21km Half Marathon', 'Full half marathon distance.', 16, NULL, 21.10, 300.00, 8000);
GO

-- Enrolments: sample participants entering events under a category
INSERT INTO Enrolments (ParticipantId, EventId, CategoryId, BibNumber, Status) VALUES
(3, 1, 1, 'CM1001', 'Confirmed'),
(4, 2, 2, 'CT2001', 'Confirmed'),
(3, 3, 3, 'SW3001', 'Confirmed'),
(4, 3, 4, 'SW4001', 'Confirmed');
GO

-- Results: sample finish data for two enrolments
INSERT INTO Results (EnrolmentId, FinishTime, Position, Status) VALUES
(1, '08:45:12', 342, 'Finished'),
(3, '00:52:30', 15, 'Finished');
GO