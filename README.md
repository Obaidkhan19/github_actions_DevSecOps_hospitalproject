# Hospital Management System

A small, working hospital management website built with ASP.NET Core, Entity Framework Core
and SQL Server. The **same code** is deployed for every hospital; each hospital has its own
configuration and its own database. There is no shared database and no central portal.

All data is synthetic. Do not enter real patient data.

## Features

- **Login and roles:** email and password, hashed passwords, session cookie, roles **Admin** and **Staff**
- **Dashboard:** patient, doctor and appointment numbers and tables, all read from the database
- **Patients:** list, search, add, edit, view, delete (delete is Admin only)
- **Doctors:** simple records (Admin adds, edits and deletes; Staff can view)
- **Appointments:** create, edit, reschedule, cancel, complete, search and filter; the same doctor cannot be double booked
- **Email notifications:** HTML emails to the patient when an appointment is created, rescheduled or cancelled
- **Users (Admin):** add staff, change role, set a new password, delete
- **Health check:** `/health` (also checks the database)

## Technology stack

C#, ASP.NET Core (.NET 10), Entity Framework Core and migrations, ASP.NET Core Identity,
SQL Server, Razor views, Bootstrap 5, a little JavaScript, SMTP, xUnit tests.

## Project structure

```
HospitalManagement/
  src/HospitalManagement.Web/
    Authentication/   role names
    Configuration/    settings classes (hospital, email, seed data)
    Controllers/      web controllers (thin, no business rules)
    Data/             database context and first-start seeding (migrations go in Data/Migrations)
    Models/           entities: Patient, Doctor, Appointment, ApplicationUser
    ViewModels/       models used by the pages
    Services/         business logic (patients, doctors, appointments, dashboard)
      Email/          email sending, templates and the appointment notifier
    Views/            Razor pages
    wwwroot/          static CSS and JavaScript
    Program.cs        application start-up
    appsettings.json  defaults without secrets
    appsettings.example.json  example values with placeholders
  tests/HospitalManagement.Tests/   automated tests
```

## Requirements

- .NET 10 SDK (check with `dotnet --version`): https://dotnet.microsoft.com/download
- A SQL Server database (see below)
- The EF Core command-line tool: `dotnet tool install --global dotnet-ef`

## SQL Server setup

Choose one:

1. **SQL Server Express LocalDB** (Windows, small). Connection string:
   `Server=(localdb)\MSSQLLocalDB;Database=HospitalDB_A;Trusted_Connection=True;TrustServerCertificate=True;`
2. **SQL Server in a container** (any system with Docker):
   ```
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YOUR_STRONG_PASSWORD>" \
     -p 1433:1433 --name hospital-a-sql -d mcr.microsoft.com/mssql/server:2022-latest
   ```
   Connection string:
   `Server=localhost,1433;Database=HospitalDB_A;User Id=sa;Password=<YOUR_STRONG_PASSWORD>;TrustServerCertificate=True;`
3. SQL Server Developer or Express installed normally.

## Environment variables

The application reads all settings from environment variables. Never commit real passwords.
Use a double underscore `__` for nested names.

| Variable | Meaning | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Database connection string (**required**) | none |
| `Hospital__Name` | Hospital name shown on the website and in emails | Demo Hospital |
| `Database__MigrateOnStartup` | Apply database migrations when the app starts | false |
| `Seed__AdminEmail` | Email of the first Admin (created only if there are no users) | empty |
| `Seed__AdminPassword` | Password of the first Admin (min. 8 characters, upper and lower case, digit, symbol) | empty |
| `Seed__AdminName` | Display name of the first Admin | Administrator |
| `Seed__SampleData` | Add synthetic sample doctors and patients if the tables are empty | false |
| `Security__SecureCookies` | Mark cookies as HTTPS-only (use `true` behind HTTPS) | false |
| `Email__Enabled` | Turn email sending on | false |
| `Email__Host` / `Email__Port` | SMTP server and port | empty / 587 |
| `Email__UseSsl` | Use TLS (STARTTLS) | true |
| `Email__Username` / `Email__Password` | SMTP login | empty |
| `Email__FromAddress` / `Email__FromName` | Sender of the emails | no-reply@hospital.example / Hospital |
| `ASPNETCORE_URLS` | Address and port the app listens on | http://localhost:5000 |

## Database migrations

The migration files are created from the models and then committed to Git.

Create the first migration (once, from the repository root). Make sure
`Database__MigrateOnStartup` is **not** set to true for this command:

```
dotnet ef migrations add InitialCreate --project src/HospitalManagement.Web --output-dir Data/Migrations
```

Apply migrations, either automatically with `Database__MigrateOnStartup=true` when the app starts,
or manually:

```
dotnet ef database update --project src/HospitalManagement.Web
```

After you change a model, add a new migration with a new name, for example `dotnet ef migrations add AddPatientNotes ...`.
Running migrations again is safe: applied migrations are skipped.

## SMTP configuration

Use a test email service first, for example a Mailtrap sandbox inbox or a local Mailpit.
Set `Email__Enabled=true`, `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`
and `Email__FromAddress`.

- If email is off, appointments work normally and nothing is sent.
- If sending fails, the appointment is still saved. The failure is logged without patient details.

## Run the application

Linux, macOS or WSL:

```
export ConnectionStrings__DefaultConnection="<your connection string>"
export Hospital__Name="Hospital A"
export Database__MigrateOnStartup=true
export Seed__AdminEmail="admin@example.com"
export Seed__AdminPassword="<a strong password>"
export Seed__SampleData=true
dotnet run --project src/HospitalManagement.Web
```

Windows PowerShell:

```
$env:ConnectionStrings__DefaultConnection = "<your connection string>"
$env:Hospital__Name = "Hospital A"
$env:Database__MigrateOnStartup = "true"
$env:Seed__AdminEmail = "admin@example.com"
$env:Seed__AdminPassword = "<a strong password>"
$env:Seed__SampleData = "true"
dotnet run --project src/HospitalManagement.Web
```

Open http://localhost:5000 and sign in with the admin email and password.
Check http://localhost:5000/health: it shows `Healthy` when the database works.

To run a second hospital, start the same code with a different `Hospital__Name`,
a different database name in the connection string, and a different port (`ASPNETCORE_URLS`).

## Run the tests

```
dotnet test tests/HospitalManagement.Tests
```

The tests use a temporary SQLite database, so they need no SQL Server and never touch your data.
They cover login and logout, permissions, patient create/read/update/delete, appointment
create/edit/reschedule/cancel, appointment validation, doctor conflicts, email triggering and the health endpoint.

## Ready for deployment (later)

- Runs on Linux. Code and configuration are separate; every setting comes from environment variables.
- The app keeps no files on disk. All data is in SQL Server, so containers can be replaced at any time.
- `/health` checks the app and the database connection.
- Logs are structured JSON on the console.
- The port comes from `ASPNETCORE_URLS`.
- Behind a reverse proxy or ingress that terminates HTTPS, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
  and `Security__SecureCookies=true`.
- Appointment times use the server's local time zone. Set the `TZ` environment variable for the hospital's time zone.
- With more than one running copy of the app, login cookies need shared data-protection keys. This is a later deployment topic.



run project 
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=HospitalDB_A;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Hospital__Name = "Hospital A"
dotnet run --project src/HospitalManagement.Web