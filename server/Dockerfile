# בנייה
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish EquipmentLoanSystem.API/EquipmentLoanSystem.API.csproj -c Release -o /app

# הרצה
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# הפלטפורמה (למשל Render) קובעת את הפורט במשתנה PORT; ברירת מחדל 8080
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ASPNETCORE_HTTP_PORTS=${PORT:-8080} dotnet EquipmentLoanSystem.API.dll
