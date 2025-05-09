TaxCloud Microservices Architecture
This project implements a microservices architecture with an API Gateway for TaxCloud services.
Architecture Overview
The solution consists of the following components:

API Gateway: Manages routing and authentication for all services
Auth Service: Handles user authentication, registration, and session management
Customer Service: Manages customer data and operations

Prerequisites

.NET 9.0 SDK
Docker and Docker Compose
SQL Server (or use the containerized version in docker-compose.yml)

Configuration
JWT Settings
Make sure the JWT settings in all services match, particularly:

Secret key
Issuer
Audience
Token validation parameters

Service URLs
The default service URLs are:

API Gateway: https://localhost:8000
Auth Service: https://localhost:5092
Customer Service: https://localhost:5094

Running the Solution
Using Docker Compose

Generate development certificates:

bash# On Windows
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p password
dotnet dev-certs https --trust

# On macOS/Linux
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p password

Start all services:

bashdocker-compose up -d

Access the API Gateway Swagger UI: https://localhost:5001/swagger

Running Locally

Start SQL Server (or use Docker):

bashdocker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrongPassword123!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

Update connection strings in each service's appsettings.json
Run each service:

bash# From the ApiGateway directory
dotnet run

# From the AuthService directory 
dotnet run

# From the CustomerService directory
dotnet run
API Endpoints
Auth Service

Authentication Flow

User logs in through the Auth Service
Auth Service verifies credentials and returns JWT token
Client includes JWT token in subsequent requests to the API Gateway
API Gateway validates the token and forwards requests to appropriate services
Services receive authenticated requests with user information in headers

Development Notes

The API Gateway validates the JWT token for all protected routes
User ID and Email are passed to downstream services via HTTP headers
Customer Service has a middleware that checks for these headers
