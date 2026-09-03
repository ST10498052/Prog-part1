# RaceDay API Endpoint Plan

**System:** RaceDay – event management platform for South African road running, walking, and cycling events.
**Roles:** `Organiser` (manages events, categories, and results), `Participant` (browses events, enrols, views own results/history). `None` = public, `Any` = any logged-in user.

This plan is scoped to the functional requirements listed in Part 2 (Authentication, User Profile, Events, Categories, Event Enrolments, Results) plus the endpoints needed to satisfy them end to end.

---

## Authentication

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as either an Organiser or a Participant. Password is hashed before storage. | None | { fullName, email, password, role, phoneNumber } | 201 Created – user id and role<br>400 Bad Request – validation failed<br>409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session, storing the user's id and role for subsequent requests. | None | { email, password } | 200 OK – session established, role returned<br>401 Unauthorized – invalid credentials |
| POST | /api/auth/logout | Ends the current session. | Any | None | 200 OK<br>401 Unauthorized |

## User Profile

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user (Organiser or Participant). | Any | None | 200 OK – user profile<br>401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any | { fullName, phoneNumber } | 200 OK – updated profile<br>400 Bad Request |
| PUT | /api/users/me/password | Changes the logged-in user's password. | Any | { currentPassword, newPassword } | 200 OK<br>401 Unauthorized – current password incorrect |

## Events

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events. Both roles can view. Supports optional filters (location, date, event type). | Any | None | 200 OK – array of events |
| GET | /api/events/{id} | Retrieves full details for a single event, including its categories. | Any | None | 200 OK – event details<br>404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in Organiser. | Organiser | { name, description, eventDate, location, distanceKm, eventType } | 201 Created – new event<br>400 Bad Request<br>403 Forbidden – not an Organiser |
| PUT | /api/events/{id} | Updates an existing event. Only the owning Organiser may edit it. | Organiser | { name, description, eventDate, location, distanceKm, eventType, status } | 200 OK – updated event<br>403 Forbidden – not the owner<br>404 Not Found |
| DELETE | /api/events/{id} | Deletes/cancels an event owned by the logged-in Organiser. | Organiser | None | 204 No Content<br>403 Forbidden<br>404 Not Found |
| GET | /api/events/mine | Lists all events created by the logged-in Organiser. | Organiser | None | 200 OK – array of events |

## Categories

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all age or distance categories available for a specific event. Both roles can view. | Any | None | 200 OK – array of categories<br>404 Not Found – event does not exist |
| POST | /api/events/{eventId}/categories | Adds a new category (e.g. "Under 20", "10km") to an event. | Organiser | { name, description, minAge, maxAge, distanceKm, entryFee, maxParticipants } | 201 Created – new category<br>403 Forbidden – not the event owner<br>404 Not Found – event does not exist |
| PUT | /api/categories/{id} | Updates an existing category's details. | Organiser | { name, description, minAge, maxAge, distanceKm, entryFee, maxParticipants } | 200 OK – updated category<br>403 Forbidden – not the event owner<br>404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event (only if it has no enrolments). | Organiser | None | 204 No Content<br>409 Conflict – category has active enrolments<br>404 Not Found |

## Event Enrolments

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Enrols the logged-in Participant into the event under a selected category, recording the link between Participant, Event, and Category, and assigning a bib number. | Participant | { categoryId } | 201 Created – enrolment with bib number<br>409 Conflict – category full or already enrolled<br>404 Not Found – event or category does not exist |
| GET | /api/users/me/enrolments | Lists all events/categories the logged-in Participant has entered (their history). | Participant | None | 200 OK – array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists all participants enrolled in an event (for the owning Organiser). | Organiser | None | 200 OK – array of enrolments<br>403 Forbidden – not the event owner |
| DELETE | /api/enrolments/{id} | Cancels/withdraws an enrolment. | Participant | None | 204 No Content<br>403 Forbidden – not the enrolment owner<br>404 Not Found |

## Results

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/result | Captures the finish time and finishing position for a participant's enrolment after the event. | Organiser | { finishTime, position, status } | 201 Created – result record<br>403 Forbidden – not the event owner<br>409 Conflict – result already captured<br>404 Not Found |
| PUT | /api/results/{id} | Corrects an existing result. | Organiser | { finishTime, position, status } | 200 OK – updated result<br>403 Forbidden – not the event owner<br>404 Not Found |
| GET | /api/events/{eventId}/results | Retrieves the full results/leaderboard for an event. Both roles can view. | Any | None | 200 OK – array of results, ordered by position |
| GET | /api/users/me/results | Retrieves the logged-in Participant's own personal performance history across all events. | Participant | None | 200 OK – array of results |

---

## Notes on role enforcement

- Every route marked `Organiser` or `Participant` must reject the other role with **403 Forbidden**, not just require *any* authenticated user.
- Ownership checks (an Organiser editing only their own events/categories/results) are enforced in addition to role checks.
- All endpoints above will be documented and testable via Swagger in Part 2, with a summary and example request/response for each.
