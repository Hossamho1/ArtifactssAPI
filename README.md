# ArtifactsAPI

A professional-grade **Artifacts Management System** built with ASP.NET Core, featuring clean architecture, microservice integration, and intelligent background processing for cultural heritage documentation and AI-powered damage analysis.

##  Overview

ArtifactsAPI is a stateless REST API that enables engineers and researchers to document, analyze, and manage archaeological and cultural artifacts. The system leverages external Python FastAPI microservices for real-time YOLO-based damage detection and COLMAP-based 3D photogrammetry, delivering responses in approximately **120ms** through intelligent asynchronous background job processing.

## Architecture

### Layered Clean Architecture
```
ArtifactsAPI (Monolithic)
├── Domain Layer (Entities, Interfaces, Enums)
├── Application Layer (DTOs, Business Logic, Services, Validators)
├── Infrastructure Layer (EF Core, Repositories, Configurations)
├── Data Layer (Database Context, Migrations)
└── API Layer (Controllers, Middlewares, Dependency Injection)
```

### Microservices Integration
- **YOLO Damage Detection**: Real-time image analysis for artifact condition assessment (hosted on Hugging Face)
- **COLMAP 3D Reconstruction**: Batch photogrammetry processing from multi-angle image sets
- **Asynchronous Job Processing**: Background tasks ensure fast API responses while offloading intensive work

##  Key Features

- **Role-Based Access Control (RBAC)**
  - Engineer: Full access to artifact creation, modification, and analysis
  - Tourist: Read-only access to published artifacts and reports

- **JWT Stateless Authentication**
  - 7-day token expiration
  - Claims-based authorization
  - Secure refresh token pattern

- **AI-Powered Analysis**
  - Real-time crack detection and severity classification
  - Automatic damage percentage calculation
  - Environmental condition monitoring (temperature, humidity)

- **3D Model Generation**
  - Batch image upload and processing
  - COLMAP-based photogrammetry
  - Background job tracking with JobId

- **Social Features**
  - Post creation with multi-media support (cover photo, 3D model)
  - Like/bookmark functionality
  - Follow system for engineers
  - View tracking per post

- **File Management**
  - Direct file upload support (up to 100MB)
  - Unique filename generation
  - Secure storage in designated folders

##  Tech Stack

### Backend
- **Framework**: ASP.NET Core 10
- **ORM**: Entity Framework Core (PostgreSQL provider)
- **Database**: PostgreSQL (Supabase)
- **Authentication**: JWT (HS256)
- **Validation**: FluentValidation
- **Mapping**: AutoMapper
- **Logging**: Serilog

### External Services
- **Python FastAPI** (YOLO Detection & COLMAP 3D)
- **Hugging Face** (Model Hosting)

### Development
- **IDE**: Visual Studio 2026
- **Language**: C# 13
- **Package Manager**: NuGet

## 📊 API Response Time

**Target: ~120ms end-to-end**
- Synchronous operations: 10-30ms
- Background job dispatch: 5-10ms
- Client response: 100-120ms total
- Background processing continues asynchronously

##  Setup Instructions

### Prerequisites
- .NET 10 SDK
- PostgreSQL 14+ (or Supabase account)
- Visual Studio 2026 or VS Code

### 1. Clone & Configure

```bash
git clone https://github.com/Hossamho1/ArtifactssAPI.git
cd ArtifactssAPI
```

### 2. Set Up Configuration

Copy the example configuration and add your credentials:

```bash
cp appsettings.Example.json appsettings.Development.json
```

Edit `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Host=your_db_host;Port=5432;Database=artifacts_db;User Id=your_db_user;Password=your_secure_password;"
  },
  "Jwt": {
	"Key": "YourSuperSecretKeyWithAtLeast32CharactersLong",
	"Issuer": "https://yourdomain.com",
	"Audience": "artifacts-api"
  }
}
```

### 3. Database Migration

```bash
dotnet ef database update --project ArtifactsAPI --startup-project ArtifactsAPI
```

### 4. Build & Run

```bash
dotnet build
dotnet run --project ArtifactsAPI
```

Navigate to `https://localhost:5001/swagger` for API documentation.

### 5. External Service Configuration

Set the Python API endpoints in `appsettings.json`:
```json
"ExternalServices": {
  "YoloDetectionUrl": "https://your-huggingface-space/analyze",
  "ColmapGenerationUrl": "https://your-huggingface-space/generate-3d"
}
```

## 📝 Typical Request Flow

### Create Post with Image Analysis
```
Client
  └─> POST /api/posts/create [FormData]
	  └─> PostsController
		  └─> PostService.CreatePostAsync()
			  ├─> Validate DTO
			  ├─> Create Artifact (automatic)
			  ├─> Save Post + Files to wwwroot
			  ├─> Trigger Background Job (AIReportService)
			  └─> Return 200 OK [~120ms]

Background Job (Async)
  └─> Send image to Python API
	  └─> Parse response (cracks, damage %)
	  └─> Store AIReport in database
	  └─> Client polls /api/posts/{id} for results
```

## Authentication

### Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"engineer@example.com","passwordHash":"password123"}'
```

### Response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "id": 1,
  "name": "Ahmed Engineer",
  "role": "Engineer",
  "canCreatePosts": true
}
```

### Protected Endpoint
```bash
curl -X GET https://localhost:5001/api/artifacts \
  -H "Authorization: Bearer {token}"
```

## Project Structure

```
ArtifactsAPI/
├── Domain/
│   ├── Models/
│   ├── Interfaces/
│   └── Enums/
├── Application/
│   ├── DTOs/
│   ├── Services/
│   ├── Interfaces/
│   ├── Validators/
│   └── Mappings/
├── Infrastructure/
│   ├── Persistence/
│   ├── Configurations/
│   ├── Repositories/
│   └── Identity/
├── Api/
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Extensions/
│   └── Program.cs
├── Migrations/
└── appsettings.*.json
```

##Contributing

For internal development, follow semantic versioning and commit messages:
```
feat: Add new feature
fix: Bug fix
docs: Documentation
refactor: Code restructuring
test: Test updates
```

##  License

This project is proprietary. Unauthorized copying or distribution is prohibited.

---

##  Developer

**Hossam Mostafa**  
*Software Development*

 .NET Backend Engineer specializing in Clean Architecture, microservice integration, and real-time data processing systems.

---

**Last Updated**: 2025-06-28  
**API Version**: 1.0.0  
**.NET Version**: 10.0
