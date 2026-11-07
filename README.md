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

## Quick customer-support chat

The authenticated customer dashboard includes Quick, a read-only support assistant. It can look up the signed-in customer's recent bookings and search approved parking spaces with availability. It cannot change bookings or process payments.

For local development, copy `backend/.env.example` to `backend/.env` and replace the placeholder with your Gemini API key. The backend loads that file when it starts; environment variables already set in the process take precedence. Alternatively, set `Gemini__ApiKey` or `GEMINI_API_KEY` in the backend environment. The backend `.gitignore` excludes `.env`, so the real key should not be committed. Do not put the key in frontend code or `frontend/.env`. Quick uses the `gemini-3.5-flash-lite` Gemini model through the C# API and returns a structured message, suggested follow-ups, and in-app parking/booking links.

The agent orchestration is implemented in C# because the application is .NET. LangChain and LangGraph do not provide official C# libraries; use a Python agent service if those libraries are mandatory.
