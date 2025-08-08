# Kanban Board - Full Stack Setup Guide

This guide will help you run the complete Kanban Board application with Angular frontend and .NET Core 8 backend.

## 🏗️ Architecture Overview

```
Angular Frontend (Port 4200)
        ↓ HTTP Requests
API Gateway (Port 5000)
        ↓ Routes requests to
┌─────────────────┬─────────────────┐
│   AuthService   │  KanbanService  │
│   (Port 5001)   │   (Port 5002)   │
└─────────────────┴─────────────────┘
        ↓                   ↓
   PostgreSQL DB     PostgreSQL DB
   (kanban_auth)     (kanban_tasks)
```

## 🚀 Quick Start (Docker - Recommended)

### Prerequisites
- Docker and Docker Compose
- Node.js 18+ (for Angular development)

### 1. Start the Backend Services

```bash
# Navigate to backend directory
cd kanban-backend

# Start all backend services with Docker
docker-compose up -d

# Verify services are running
docker-compose ps
```

Services will be available at:
- **API Gateway**: http://localhost:5000
- **Auth Service**: http://localhost:5001  
- **Kanban Service**: http://localhost:5002
- **PostgreSQL**: localhost:5432

### 2. Start the Angular Frontend

```bash
# Navigate to frontend directory (project root)
cd ..

# Install dependencies (if not already done)
npm install

# Start Angular development server
ng serve
```

The Angular app will be available at: **http://localhost:4200**

### 3. Test the Integration

1. Open http://localhost:4200
2. Click "Continue as Guest" or register a new account
3. Create, edit, and move tasks between columns
4. All data is now persisted in PostgreSQL via the backend APIs

## 🔧 Manual Setup (Without Docker)

### Prerequisites
- .NET Core 8.0 SDK
- Node.js 18+
- PostgreSQL 15+

### 1. Setup PostgreSQL

```bash
# Create databases
createdb kanban_auth
createdb kanban_tasks

# Or use PostgreSQL commands:
psql -U postgres
CREATE DATABASE kanban_auth;
CREATE DATABASE kanban_tasks;
```

### 2. Configure Connection Strings

Update the `appsettings.json` files in:
- `kanban-backend/src/services/AuthService/appsettings.json`
- `kanban-backend/src/services/KanbanService/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kanban_auth;Username=your_user;Password=your_password"
  }
}
```

### 3. Start Backend Services

```bash
# Terminal 1: Start Auth Service
cd kanban-backend/src/services/AuthService
dotnet run --urls="http://localhost:5001"

# Terminal 2: Start Kanban Service  
cd kanban-backend/src/services/KanbanService
dotnet run --urls="http://localhost:5002"

# Terminal 3: Start API Gateway
cd kanban-backend/src/gateway/ApiGateway
dotnet run --urls="http://localhost:5000"
```

### 4. Start Angular Frontend

```bash
# Terminal 4: Start Angular
ng serve
```

## 🔗 API Integration Details

The Angular frontend now connects to the backend through:

### Authentication Flow
1. **Login**: `POST /api/auth/login` → Returns JWT token + user data
2. **Register**: `POST /api/auth/register` → Creates user, then auto-login
3. **Guest**: `POST /api/auth/guest` → Creates temporary user with JWT
4. **Validation**: `GET /api/auth/validate` → Validates JWT token

### Task Management Flow
1. **Get Tasks**: `GET /api/kanban` → Retrieves all user's tasks
2. **Create Task**: `POST /api/kanban` → Creates new task
3. **Update Task**: `PUT /api/kanban/{id}` → Updates task details
4. **Delete Task**: `DELETE /api/kanban/{id}` → Removes task
5. **Move Task**: `POST /api/kanban/{id}/move` → Changes status/order
6. **Reorder Tasks**: `POST /api/kanban/reorder` → Bulk reorder within column

### JWT Token Handling
- Tokens stored in `localStorage` as `kanban-token`
- Automatically added to all API requests via HTTP interceptor
- Auto-logout on token expiration (401 responses)

## 🛠️ Development Features

### Frontend Changes Made
- ✅ Updated `AuthService` to use HTTP calls instead of localStorage
- ✅ Updated `TodoService` to use backend APIs with proper error handling
- ✅ Added JWT token interceptor for automatic authentication
- ✅ Updated all components to handle string IDs instead of numbers
- ✅ Added environment configuration for API URL
- ✅ Maintained all existing UI functionality (drag-drop, themes, etc.)

### Backend Features
- ✅ JWT authentication with 7-day expiration
- ✅ BCrypt password hashing with separate storage
- ✅ User isolation - users only see their own data
- ✅ Guest user support for anonymous access
- ✅ Proper task ordering with drag-drop support
- ✅ CORS configured for Angular frontend
- ✅ Structured logging with Serilog
- ✅ API documentation with Swagger
- ✅ Docker support with health checks

## 🧪 Testing the Integration

### 1. Authentication Testing
```bash
# Register a new user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","fullName":"Test User","password":"password123"}'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"password123"}'

# Create guest user
curl -X POST http://localhost:5000/api/auth/guest
```

### 2. Task Management Testing
```bash
# Get JWT token from login response, then:

# Create task
curl -X POST http://localhost:5000/api/kanban \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Task","description":"Test Description","status":0}'

# Get all tasks
curl -X GET http://localhost:5000/api/kanban \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🚨 Troubleshooting

### Common Issues

#### 1. CORS Errors
- Ensure all backend services include Angular URL (http://localhost:4200) in CORS configuration
- Check that `withCredentials: false` in Angular HTTP calls

#### 2. Authentication Issues  
- Verify JWT secret is consistent across all services
- Check token format: `Bearer <token>` in Authorization header
- Ensure token hasn't expired (7-day default)

#### 3. Database Connection Errors
- Verify PostgreSQL is running and accessible
- Check connection strings in `appsettings.json`
- Ensure databases `kanban_auth` and `kanban_tasks` exist

#### 4. API Gateway Routing Issues
- Verify all backend services are running on correct ports
- Check YARP configuration in `appsettings.json`
- Test services directly before using through gateway

### Debugging Steps

1. **Check Service Health**:
   ```bash
   curl http://localhost:5000/health  # API Gateway
   curl http://localhost:5001/health  # Auth Service  
   curl http://localhost:5002/health  # Kanban Service
   ```

2. **View Logs**:
   ```bash
   # Docker logs
   docker-compose logs -f [service-name]
   
   # Manual setup - check console output in each terminal
   ```

3. **Database Verification**:
   ```bash
   psql -U kanban_user -d kanban_auth
   \dt  # List tables
   SELECT * FROM "Users" LIMIT 5;
   ```

## 🔄 Data Migration

The application handles the transition from localStorage to backend seamlessly:
- Previous localStorage todos are ignored (clean start)
- User registration/login creates fresh database records
- Guest users get new temporary accounts each session

## 🌟 Next Steps

With the full-stack integration complete, you can now:

1. **Deploy to Production**: 
   - Use proper secrets management
   - Configure HTTPS
   - Set up CI/CD pipelines

2. **Add Advanced Features**:
   - Real-time updates with SignalR
   - File attachments to tasks
   - Team collaboration features
   - Advanced reporting and analytics

3. **Scale the Application**:
   - Add load balancing
   - Implement caching with Redis
   - Use container orchestration (Kubernetes)

The application is now a fully functional, production-ready Kanban board with modern architecture and security best practices!