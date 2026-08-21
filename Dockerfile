FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY nuget.config ./
COPY Hemo.Pdf.sln ./
COPY src/Hemo.Pdf.Api/Hemo.Pdf.Api.csproj src/Hemo.Pdf.Api/
COPY src/Hemo.Pdf.Application/Hemo.Pdf.Application.csproj src/Hemo.Pdf.Application/
COPY src/Hemo.Pdf.Core/Hemo.Pdf.Core.csproj src/Hemo.Pdf.Core/
COPY src/Hemo.Pdf.Branding/Hemo.Pdf.Branding.csproj src/Hemo.Pdf.Branding/
COPY src/Hemo.Pdf.Sections/Hemo.Pdf.Sections.csproj src/Hemo.Pdf.Sections/
COPY src/Hemo.Pdf.Layouts/Hemo.Pdf.Layouts.csproj src/Hemo.Pdf.Layouts/
COPY src/Hemo.Pdf.Rendering/Hemo.Pdf.Rendering.csproj src/Hemo.Pdf.Rendering/

RUN dotnet restore src/Hemo.Pdf.Api/Hemo.Pdf.Api.csproj

COPY src/ src/
COPY assets/ assets/
COPY packages/ packages/

WORKDIR /src/src/Hemo.Pdf.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5090
EXPOSE 5090

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Hemo.Pdf.Api.dll"]
