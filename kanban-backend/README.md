# Kanban Board Backend - .NET Core 8 Microservices

This is a .NET Core 8 microservices backend for the Kanban Board application. It consists of multiple services:

- **AuthService**: Handles user authentication and authorization
- **KanbanService**: Manages kanban tasks and boards
- **ApiGateway**: Acts as a reverse proxy and entry point for all APIs
- **Shared Libraries**: Common models and utilities

## Architecture

```
Frontend (Angular) 
    ↓
API Gateway (Port 5000)
    ↓
┌─────────────────┬─────────────────┐
│   AuthService   │  KanbanService  │
│   (Port 5001)   │   (Port 5002)   │
└─────────────────┴─────────────────┘
    ↓                       ↓
PostgreSQL (kanban_auth)    PostgreSQL (kanban_tasks)
```

## Prerequisites

- .NET Core 8.0 SDK
- PostgreSQL 15+
- Docker & Docker Compose (optional)

## Getting Started

### Option 1: Using Docker Compose (Recommended)

1. Clone the repository and navigate to the backend directory:
   ```bash
   cd kanban-backend
   ```

2. Start all services with Docker Compose:
   ```bash
   docker-compose up -d
   ```

3. The services will be available at:
   - API Gateway: http://localhost:5000
   - Auth Service: http://localhost:5001
   - Kanban Service: http://localhost:5002
   - PostgreSQL: localhost:5432

4. Access Swagger documentation:
   - Gateway: http://localhost:5000/swagger
   - Auth Service: http://localhost:5001/swagger
   - Kanban Service: http://localhost:5002/swagger

### Option 2: Manual Setup

1. **Setup PostgreSQL:**
   ```bash
   # Create databases
   createdb kanban_auth
   createdb kanban_tasks
   ```

2. **Update Connection Strings:**
   Update `appsettings.json` files in each service with your PostgreSQL connection details.

3. **Build and Run Services:**
   ```bash
   # Build the solution
   dotnet build KanbanBoard.sln

   # Run Auth Service
   cd src/services/AuthService
   dotnet run --urls="http://localhost:5001"

   # Run Kanban Service (in new terminal)
   cd src/services/KanbanService
   dotnet run --urls="http://localhost:5002"

   # Run API Gateway (in new terminal)
   cd src/gateway/ApiGateway
   dotnet run --urls="http://localhost:5000"
   ```

## API Endpoints

### Authentication Service (via Gateway: `/api/auth`)

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/guest` - Create guest user
- `GET /api/auth/validate` - Validate JWT token
- `GET /api/auth/user/{userId}` - Get user by ID

### Kanban Service (via Gateway: `/api/kanban`)

- `GET /api/kanban` - Get all tasks for current user
- `GET /api/kanban/{taskId}` - Get specific task
- `POST /api/kanban` - Create new task
- `PUT /api/kanban/{taskId}` - Update task
- `DELETE /api/kanban/{taskId}` - Delete task
- `POST /api/kanban/{taskId}/move` - Move task to different status/order
- `POST /api/kanban/reorder` - Reorder tasks within a status

## Database Schema

### Users Table (Auth Service)
- Id (UUID, PK)
- Username (String, Unique)
- Email (String, Unique)
- FullName (String)
- Avatar (String, nullable)
- IsGuest (Boolean)
- CreatedAt (DateTime)

### UserPasswords Table (Auth Service)
- Id (UUID, PK)
- UserId (UUID, FK to Users)
- PasswordHash (String)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)

### Tasks Table (Kanban Service)
- Id (UUID, PK)
- Title (String)
- Description (String)
- Status (Integer: 0=Todo, 1=InProgress, 2=Done)
- Order (Integer)
- UserId (UUID, FK to Users)
- CreatedAt (DateTime)
- UpdatedAt (DateTime, nullable)

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `Jwt__Secret`: JWT signing secret (minimum 32 characters)

### JWT Configuration

The JWT secret must be the same across all services. In production:
1. Use a strong, randomly generated secret (minimum 32 characters)
2. Store it securely (Azure Key Vault, AWS Secrets Manager, etc.)
3. Never commit secrets to version control

## Development

### Project Structure
```
kanban-backend/
├── src/
│   ├── services/
│   │   ├── AuthService/         # Authentication microservice
│   │   └── KanbanService/       # Task management microservice
│   ├── gateway/
│   │   └── ApiGateway/          # YARP reverse proxy
│   └── shared/
│       ├── KanbanBoard.Shared/  # Shared models and DTOs
│       └── KanbanBoard.Common/  # Common utilities and middleware
├── tests/                       # Unit and integration tests
├── scripts/                     # Database scripts
└── docker-compose.yml
```

### Adding New Services

1. Create new project in `src/services/`
2. Add project reference to solution file
3. Create Dockerfile for the service
4. Update docker-compose.yml with new service
5. Add routes to API Gateway configuration

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/AuthService.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Security Features

- JWT-based authentication
- BCrypt password hashing
- CORS configuration
- Request validation
- Exception handling middleware
- User isolation (users can only access their own data)
- Guest user support for anonymous access

## Logging

All services use Serilog for structured logging:
- Console logging (development)
- File logging (`logs/` directory)
- PostgreSQL logging (production, optional)

## Troubleshooting

### Common Issues

1. **Database Connection Errors:**
   - Ensure PostgreSQL is running
   - Check connection strings in appsettings.json
   - Verify database exists

2. **JWT Token Issues:**
   - Ensure JWT secret is consistent across services
   - Check token expiration
   - Verify Authorization header format: `Bearer {token}`

3. **CORS Issues:**
   - Update CORS origins in each service's Program.cs
   - Ensure Angular app URL is included

4. **Docker Issues:**
   - Run `docker-compose down -v` to remove volumes
   - Check service health with `docker-compose ps`
   - View logs with `docker-compose logs [service-name]`

### Health Checks

Each service exposes health check endpoints:
- http://localhost:5001/health (Auth Service)
- http://localhost:5002/health (Kanban Service)
- http://localhost:5000/health (API Gateway)

## Production Deployment

1. **Security:**
   - Use strong JWT secrets
   - Enable HTTPS
   - Configure proper CORS origins
   - Use connection string secrets management

2. **Database:**
   - Use managed PostgreSQL service
   - Enable SSL connections
   - Setup database backups
   - Configure connection pooling

3. **Monitoring:**
   - Setup application insights
   - Configure health checks
   - Monitor performance metrics
   - Setup alerting

4. **Scaling:**
   - Use container orchestration (Kubernetes)
   - Setup load balancers
   - Configure auto-scaling
   - Use distributed caching (Redis)