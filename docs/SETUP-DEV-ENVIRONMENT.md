# Setting Up Development Environment

This repository provides a script to set up the development environment for new developers. It ensures that all required .NET tools specified in the `.config/dotnet-tools.json` file are restored.

## When to Run This Script

You should run the `setup-dev-environment.ps1` script:
1. **After cloning the repository**: To ensure all required tools and configurations are set up.
2. **Whenever the `.config/dotnet-tools.json` file is updated**: To restore any new tools added to the project.


## Steps to Set Up

1. Open a terminal (e.g., PowerShell) in the root of the repository.
2. Run the setup script:
   
   ```powershell
   ./setup-dev-environment.ps1
   ```

### What the Script Does?
- Restores .NET tools specified in the `.config/dotnet-tools.json` file.
- Installs CSharpier.
- Installs Husky to manage Git hooks (e.g., pre-commit hooks).

>[!Note]
> Ensure you have the necessary permissions to execute PowerShell scripts. If you encounter an error, you may need to enable script execution by running:
  ```powershell
    Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
  ```

### Why do we need Husky?
Husky is used to enforce consistent formatting of .cs files by running tools like CSharpier during Git pre-commit hooks. This ensures that all committed code adheres to the project's formatting standards.
