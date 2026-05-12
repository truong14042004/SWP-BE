# SWP-BE

ASP.NET Core Web API (.NET 8) backend with Google login, JWT authentication, and PostgreSQL via Entity Framework Core.

## Google Login API

Frontend logs in with Google, then sends the Google `id_token` to:

```http
POST /api/auth/google
Content-Type: application/json

{
  "idToken": "GOOGLE_ID_TOKEN_FROM_FE"
}
```

The API verifies the token with Google, creates or updates the user, then returns the app JWT.

## Required Configuration

Update `appsettings.json`, user secrets, or environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=swpsu26;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID"
    }
  },
  "Jwt": {
    "Secret": "YOUR_AT_LEAST_32_CHARACTERS_SECRET"
  }
}
```

Cloud SQL instance connection name:

```text
project-e0e65bea-54d9-45cc-83b:asia-northeast1:swpsu26
```

This is the Google Cloud SQL instance name, not a PostgreSQL connection string by itself.

### Option 1: Connect to Cloud SQL by IP

Use this if the Cloud SQL instance has a public IP or your backend is deployed in the same VPC and can reach the private IP:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_CLOUD_SQL_IP;Port=5432;Database=swpsu26;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Option 2: Connect to Cloud SQL through Cloud SQL Auth Proxy

Use this for local development or servers where you want IAM-authenticated tunneling to the cloud database. PostgreSQL is still running on Cloud SQL; `127.0.0.1:5432` is only the local tunnel endpoint.

```powershell
cloud-sql-proxy project-e0e65bea-54d9-45cc-83b:asia-northeast1:swpsu26 --port 5432
```

Then use:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=swpsu26;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD"
  }
}
```

## Database

After creating the PostgreSQL database and setting the real connection string:

```powershell
dotnet ef database update
```

## Run

```powershell
dotnet run
```
