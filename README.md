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