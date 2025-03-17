#!/bin/bash

# Use the specific dotnet path
DOTNET_CMD="/usr/bin/dotnet"

echo "Using dotnet at: $DOTNET_CMD"
echo "Installing packages for Firebase Authentication and Polly Resilience..."

# Firebase packages
echo "Installing Firebase packages..."
$DOTNET_CMD add BackendShop.API/BackendShop.API.csproj package FirebaseAdmin
$DOTNET_CMD add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package FirebaseAdmin

# Polly packages for resilience
echo "Installing Polly packages..."
$DOTNET_CMD add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package Polly
$DOTNET_CMD add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package Polly.Extensions.Http
$DOTNET_CMD add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package Microsoft.Extensions.Http
$DOTNET_CMD add BackendShop.API/BackendShop.API.csproj package Microsoft.Extensions.Http.Polly

# Additional required packages
echo "Installing additional required packages..."
$DOTNET_CMD add BackendShop.Infrastructure/BackendShop.Infrastructure.csproj package Microsoft.Extensions.Options
$DOTNET_CMD add BackendShop.API/BackendShop.API.csproj package Microsoft.AspNetCore.RateLimiting 2>/dev/null || echo "Note: RateLimiting package may not be available in your .NET version. This is okay for .NET 8."
$DOTNET_CMD add BackendShop.API/BackendShop.API.csproj package Swashbuckle.AspNetCore.Annotations

# Restore packages
echo "Restoring all packages..."
$DOTNET_CMD restore

echo "All packages installed successfully!"
echo "Note: If you see any errors about Microsoft.AspNetCore.RateLimiting, you can ignore them for .NET 8 as rate limiting is included."