#!/bin/bash

# Create a solution
dotnet new sln -n BackendShop

# Create projects
dotnet new webapi -n BackendShop.API
dotnet new classlib -n BackendShop.Core
dotnet new classlib -n BackendShop.Infrastructure
dotnet new xunit -n BackendShop.Tests

# Add projects to solution
dotnet sln add BackendShop.API/BackendShop.API.csproj
dotnet sln add BackendShop.Core/BackendShop.Core.csproj
dotnet sln add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj
dotnet sln add BackendShop.Tests/BackendShop.Tests.csproj

# Set up project references
dotnet add BackendShop.API/BackendShop.API.csproj reference BackendShop.Core/BackendShop.Core.csproj
dotnet add BackendShop.API/BackendShop.API.csproj reference BackendShop.Infrastructure/BackendShop.Infrastructure.csproj
dotnet add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj reference BackendShop.Core/BackendShop.Core.csproj
dotnet add BackendShop.Tests/BackendShop.Tests.csproj reference BackendShop.Core/BackendShop.Core.csproj
dotnet add BackendShop.Tests/BackendShop.Tests.csproj reference BackendShop.Infrastructure/BackendShop.Infrastructure.csproj
dotnet add BackendShop.Tests/BackendShop.Tests.csproj reference BackendShop.API/BackendShop.API.csproj

# Add basic packages
dotnet add BackendShop.API/BackendShop.API.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer
dotnet add BackendShop.API/BackendShop.API.csproj package FirebaseAdmin

# Create core folders
mkdir -p BackendShop.Core/Entities
mkdir -p BackendShop.Core/Interfaces
mkdir -p BackendShop.Core/Enums

# Create infrastructure folders
mkdir -p BackendShop.Infrastructure/Data
mkdir -p BackendShop.Infrastructure/Repositories
mkdir -p BackendShop.Infrastructure/Services

# Create API folders
mkdir -p BackendShop.API/Controllers
mkdir -p BackendShop.API/Middleware
mkdir -p BackendShop.API/Services
mkdir -p BackendShop.API/Models/DTOs

echo "Project structure setup complete!"