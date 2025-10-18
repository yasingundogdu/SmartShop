FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/order/Order.Publisher/Order.Publisher.csproj src/order/Order.Publisher/
RUN dotnet restore src/order/Order.Publisher/Order.Publisher.csproj
COPY . .
RUN dotnet publish src/order/Order.Publisher/Order.Publisher.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Publisher.dll"]
