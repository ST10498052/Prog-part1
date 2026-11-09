RaceDay

Module: PROG6212 – Portfolio of Evidence Student: ST10498052 Repository: https://github.com/ST10498052/Prog-part1 (Part 3 work is on the part3 branch)

View GitHub Actions build runs

RaceDay is an event management platform for running, walking and cycling events. Organisers create events, define categories and capture results. Participants browse events, enrol under a category and view their own race history.

The two roles
Organiser
Dashboard: events created, total enrolments, enrolments per event and upcoming dates
Create, edit and delete their own events (name, description, date, location, distance, event type), with a banner image upload
Add age or distance categories to an event (e.g. Under 20, Senior, 10 km, 21 km)
View everyone enrolled for an event, with category and status
Capture finish times and finishing positions; saved results are published to participants immediately
Participant
Home page of upcoming events with filters (location, event type, date)
Event details page with banner and available categories
Enrol by choosing a category (recorded immediately, bib number assigned)
My enrolments with colour-coded statuses, and withdraw
My results: event, date, category, finish time and position (e.g. "87th out of 512 finishes")
Profile page: update personal information and upload a profile picture

Each role gets its own navigation and pages. Organiser pages are not reachable by Participants and vice versa (the MVC app redirects to an "access denied" page and the API returns 403). Anonymous users are redirected to the login page (the API returns 401).

Part 1 – Planning and database
ERD: docs/RaceDay_ERD.png
Endpoint plan: docs/RaceDay_Endpoint_Plan.md
SQL script: docs/RaceDay_Database.sql

ERD

Table	Purpose
Roles	Lookup: Organiser, Participant
Users	Accounts (hashed password, role, optional profile picture)
Events	Name, description, date, location, distance, event type, owner, optional banner
Categories	Age or distance categories belonging to an event
Enrolments	Links a Participant, an Event and the chosen Category; unique bib number
Results	One result per enrolment: finish time, position, status

Part 3 schema additions (created automatically by the EF Core migration): Events.BannerImage and Users.ProfilePicture (both nvarchar(300), nullable). They hold the blob name of the image in Azure Blob Storage.

Part 2 – REST API

ASP.NET Core Web API (.NET 8), Entity Framework Core Code-First on SQL Server, session-based authentication, Swagger.

Area	Method and route	Role
Auth	POST /api/auth/register, POST /api/auth/login, POST /api/auth/logout	Public / any
Profile	GET/PUT /api/users/me, PUT /api/users/me/password	Any logged in
	POST /api/users/me/profile-picture (multipart)	Any logged in
	GET /api/users/me/enrolments, GET /api/users/me/results	Participant
Events	GET /api/events, GET /api/events/{id}	Any logged in
	GET /api/events/mine, POST /api/events	Organiser
	PUT/DELETE /api/events/{id}	Organiser (owner)
	POST /api/events/{id}/banner (multipart)	Organiser (owner)
Categories	GET /api/events/{eventId}/categories	Any logged in
	POST /api/events/{eventId}/categories, PUT/DELETE /api/categories/{id}	Organiser (owner)
Enrolments	POST /api/events/{eventId}/enrolments, DELETE /api/enrolments/{id}	Participant
	GET /api/events/{eventId}/enrolments	Organiser (owner)
Results	POST /api/enrolments/{id}/result, PUT /api/results/{id}	Organiser (owner)
	GET /api/events/{eventId}/results	Any logged in
Files	GET /api/files/{blobName} (images are streamed through the API)	Any logged in

Every endpoint is documented in Swagger (purpose, expected data, responses). Passwords are hashed with PBKDF2 and a random salt. Login stores the user id and role in a server-side session; [SessionAuthorize] returns 401 when not logged in and 403 for the wrong role.

Azure Blob Storage

Event banners and profile pictures are stored in a private Azure Blob Storage container (raceday-images). Uploads are validated (JPEG, PNG, GIF or WebP, up to 5 MB, and the file signature is checked). Images are never exposed through a public Azure URL: the browser asks the MVC app for /files/..., the MVC app asks the API, and the API reads the blob.

Part 3 – MVC web application

ASP.NET Core MVC (.NET 8) with Bootstrap 5 and a custom sporting theme (navy, electric orange and lime; Barlow Condensed headings).

Session management: after login the MVC app stores the user's id, role and the API's session cookie in its own session, and sends that cookie on every API call
Role-specific navigation and pages, protected by an [AppAuthorize] filter
Friendly error handling: server-side validation messages on every form, and API failures are shown as readable messages or error pages, never raw exceptions
Colour-coded statuses: green = Confirmed/Published, amber = Pending/Draft, red = Cancelled, blue = Completed, grey = other
