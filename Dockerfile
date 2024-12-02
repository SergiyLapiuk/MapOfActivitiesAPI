FROM centos:7 AS base

# Add Microsoft package repository and install ASP.NET Core
RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm \
    && yum install -y aspnetcore-runtime-6.0

# Ensure we listen on any IP Address 
ENV DOTNET_URLS=http://+:44333

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY MapOfActivitiesAPI/*.csproj ./
RUN dotnet restore

COPY MapOfActivitiesAPI/ .          
RUN dotnet build -c Release -o /app/build


FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MapOfActivitiesAPI.dll"]
