-- Create databases for the microservices
CREATE DATABASE kanban_auth;
CREATE DATABASE kanban_tasks;

-- Grant permissions to the kanban_user (user is already created by POSTGRES_USER env var)
GRANT ALL PRIVILEGES ON DATABASE kanban_auth TO kanban_user;
GRANT ALL PRIVILEGES ON DATABASE kanban_tasks TO kanban_user;