# QuickPark Backend

This is the backend service for the QuickPark application. It handles the core API routes, data persistence, and authentication for the system.

## About the Frontend
The frontend of this application is built separately using React and Tailwind CSS. The frontend and backend communicate exclusively through standard HTTP requests using the native Fetch API.

## Tech Stack
- **Framework:** .NET 10 (ASP.NET Core Web API)
- **Database:** SQL Server
- **ORM:** Entity Framework Core
- **Authentication:** JWT Bearer tokens
- **Security:** BCrypt for password hashing

## Getting Started

To get the backend running on your local machine:

1. Ensure you have the .NET 10 SDK installed.
2. Open your terminal in the `backend` directory.
3. Check the `appsettings.json` or `appsettings.Development.json` file and make sure your SQL Server connection string is correct.
4. Apply the database migrations to set up your local database:
   ```bash
   dotnet ef database update
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

The API will spin up and start listening for requests from the frontend.
