# Hospital Management System – DevSecOps

A web-based Hospital Management System developed using **ASP.NET Core (.NET 10), Entity Framework Core, and Microsoft SQL Server**. The project focuses on integrating DevSecOps practices into the software development lifecycle using GitHub Actions.

## DevSecOps Implementation

Implemented independent GitHub Actions workflows to automate build validation, code quality analysis, and security testing.

### 1. Continuous Integration (CI)
- Automated application builds using GitHub Actions.
- Integrated automated unit testing with xUnit.
- Validates code changes to help prevent broken builds.

### 2. Static Application Security Testing (SAST)
- Integrated **GitHub CodeQL** to analyze C# source code.
- Detects potential security vulnerabilities and insecure coding patterns.
- Automates source-code security analysis.

### 3. Secret Scanning
- Integrated **Gitleaks** to detect accidentally exposed secrets.
- Scans the repository for API keys, passwords, access tokens, and other sensitive information.

### 4. Dependency Scanning
- Integrated **NuGet Audit** to identify vulnerable NuGet packages.
- Used **GitHub Dependency Review** to check dependency changes for known vulnerabilities.

### 5. Code Quality Analysis
- Integrated **SonarQube Cloud** for automated code analysis.
- Checks code quality, maintainability, code smells, and security-related issues.
- Used `dotnet format` to validate code formatting and consistency.

### 6. Dynamic Application Security Testing (DAST)
- Integrated **OWASP ZAP** for automated baseline security testing.
- Scans a running application for common web security issues.

### 7. Infrastructure and Filesystem Security Scanning
- Integrated **Trivy** to scan project files for known vulnerabilities and security misconfigurations.

## GitHub Actions Workflows

- Created separate YAML workflow files under `.github/workflows/`.
- Each security and quality check can run independently.
- Configured automated triggers for workflow execution.
- Added scheduled and manual execution where appropriate.
- Used GitHub Secrets and repository variables to manage sensitive configuration.

## Technology Stack

- **Backend:** C#, ASP.NET Core (.NET 10)
- **Database:** Microsoft SQL Server, Entity Framework Core
- **CI/CD:** GitHub Actions, YAML
- **SAST:** GitHub CodeQL
- **Secret Scanning:** Gitleaks
- **Dependency Scanning:** NuGet Audit, GitHub Dependency Review
- **DAST:** OWASP ZAP
- **Vulnerability Scanning:** Trivy
- **Code Quality:** SonarQube Cloud, dotnet format
- **Testing:** xUnit

## Project Objective

The objective of this project is to demonstrate how security can be integrated throughout the software development lifecycle rather than being treated as a final step.

It combines automated CI, source-code analysis, secret detection, dependency vulnerability checks, dynamic security testing, and filesystem scanning to establish a practical DevSecOps workflow for a .NET application.



## Docker Implementation

### Containerization
- Implemented a multi-stage Docker build for the ASP.NET Core Hospital Management application.
- Configured two Docker images and two containers: Hospital Web and SQL Server Database.
- Configured Docker Compose to manage the application services.

### Persistent Storage
- Implemented Docker named volumes for persistent data:
  - `hospital_sqldata` – SQL Server database files.
  - `hospital_dpkeys` – ASP.NET Core Data Protection keys.
- SQL Server volume host location: `/var/lib/docker/volumes/hospital_sqldata/_data`.
- Docker volumes are better for persistent application data, while bind mounts are generally better for development workflows.

### Docker Hub
- Published the Hospital Web image to Docker Hub.
- Configured Docker Compose to pull and run the published image instead of building it locally.
- Implemented image versioning:
  - `mukhann/hospital-web:v1`
  - `mukhann/hospital-web:v2`
- Tested updating the application, building a new image version, pushing it to Docker Hub and deploying the updated version through Docker Compose.

### Database Backup and Restore
- Implemented SQL Server database backup using `.bak` files.
- Copied database backups from the SQL Server container to the Ubuntu host.
- Successfully restored the backup into a separate test database named `HospitalDB_RestoreTest`.
- Verified that both the original and restored databases were available.
