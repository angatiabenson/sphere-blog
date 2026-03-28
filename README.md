![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-CC2927?logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=dotnet&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT%20Bearer-000000?logo=jsonwebtokens&logoColor=white)
![Serilog](https://img.shields.io/badge/Logging-Serilog-2C8EBB)
[![CI](https://github.com/angatiabenson/sphere-blog/actions/workflows/ci.yml/badge.svg)](https://github.com/angatiabenson/sphere-blog/actions/workflows/ci.yml)

# Sphere Blog

A simple blog engine REST API built with ASP.NET 10. Users register, write blog posts with tags, and comment on posts. JWT authentication, structured logging with correlation IDs, and rate limiting included out of the box.

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|:----:|-------------|
| `POST` | `/api/auth/register` | | Create account |
| `POST` | `/api/auth/login` | | Login, get JWT |
| `GET` | `/api/blogs` | | List blogs (paginated) |
| `GET` | `/api/blogs/{id}` | | Get blog with comments |
| `POST` | `/api/blogs` | Yes | Create blog |
| `PUT` | `/api/blogs/{id}` | Owner | Update blog |
| `DELETE` | `/api/blogs/{id}` | Owner | Delete blog |
| `POST` | `/api/blogs/{blogId}/comments` | Yes | Add comment |
| `DELETE` | `/api/blogs/{blogId}/comments/{commentId}` | Owner | Delete comment |
| `GET` | `/api/tags` | | List all tags |
| `GET` | `/api/tags/{name}/blogs` | | Blogs by tag |

**Response format:**

```json
{ "status": "success", "code": 200, "data": { } }
{ "status": "error", "code": 400, "message": "...", "reference": "correlation-id" }
```

## Getting Started

**Prerequisites:** .NET 10 SDK, SQL Server LocalDB

```bash
git clone https://github.com/angatiabenson/sphere-blog.git
cd sphere-blog

# Set JWT secret for dev
cd src
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your-secret-key-at-least-32-characters-long!!"

# Run (DB migrates automatically on startup)
dotnet run
```

API docs available at `/docs` (Scalar).

## Running Tests

```bash
dotnet test
```

19 integration tests using in-memory database — no SQL Server needed.

## Project Structure

```
src/
├── Domain/Entities/          # User, Blog, Tag, BlogTag, Comment
├── Application/              # Interfaces, Services, DTOs
├── Infrastructure/Data/      # EF Core DbContext, configs, migrations
└── API/                      # Controllers, Middleware, Response models
```

## Deployment (IIS)

```bash
dotnet publish src -c Release -o ./publish
```

Set `Jwt__Secret` as an environment variable on the server. The included `web.config` is configured for IIS in-process hosting.
