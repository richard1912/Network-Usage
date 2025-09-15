@echo off
setlocal enabledelayedexpansion

:: Network-Usage Update Pusher Script (Windows Version)
:: This script helps push updates to the develop branch of the Network-Usage repository

:: Main script starts here
echo Starting Network-Usage Update Process...
echo.

:: Check if we're in a git repository
if not exist ".git" (
    echo [ERROR] Not in a git repository. Please run this script from the Network-Usage project directory.
    pause
    exit /b 1
)

:: Set remote origin to development repository
echo [INFO] Setting remote origin to development repository...
git remote set-url origin https://github.com/richard1912/Network-Usage.git
if errorlevel 1 (
    echo [ERROR] Failed to set remote origin.
    pause
    exit /b 1
) else (
    echo [SUCCESS] Remote origin set to development repository.
)

:: Check if git is installed
git --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Git is not installed. Please install git first.
    pause
    exit /b 1
)

:: Get current branch
for /f "tokens=*" %%i in ('git branch --show-current') do set current_branch=%%i
echo [INFO] Current branch: !current_branch!

:: Check if there are any changes to commit
git status --porcelain >nul 2>&1
if errorlevel 1 (
    echo [WARNING] No changes detected. Nothing to commit.
    pause
    exit /b 0
)

:: Check if there are actually changes
for /f %%i in ('git status --porcelain ^| find /c /v ""') do set change_count=%%i
if !change_count! equ 0 (
    echo [WARNING] No changes detected. Nothing to commit.
    pause
    exit /b 0
)

:: Show current status
echo [INFO] Current git status:
git status --short

:: Prompt for commit message
echo.
echo Please provide a description of your changes:
echo This will be used as the commit message.
echo.
set /p commit_message="Description: "

:: Check if commit message is empty
if "!commit_message!"=="" (
    echo [ERROR] Commit message cannot be empty. Exiting.
    pause
    exit /b 1
)

:: Confirm the action
echo.
echo About to commit and push with message:
echo   '!commit_message!'
echo.
set /p confirm="Proceed? (y/N): "

if /i not "!confirm!"=="y" (
    echo [INFO] Operation cancelled.
    pause
    exit /b 0
)

:: Add all changes
echo [INFO] Adding all changes...
git add .
if errorlevel 1 (
    echo [ERROR] Failed to add changes.
    pause
    exit /b 1
)

:: Commit changes
echo [INFO] Committing changes...
git commit -m "!commit_message!"
if errorlevel 1 (
    echo [ERROR] Failed to commit changes.
    pause
    exit /b 1
) else (
    echo [SUCCESS] Changes committed successfully!
)

:: Check if we need to switch to develop branch
if not "!current_branch!"=="develop" (
    echo [WARNING] You're not on the develop branch. Switching to develop...
    git checkout develop
    if errorlevel 1 (
        echo [ERROR] Failed to switch to develop branch.
        pause
        exit /b 1
    ) else (
        echo [SUCCESS] Switched to develop branch.
    )
    
    :: Merge the changes from the previous branch
    echo [INFO] Merging changes from !current_branch!...
    git merge "!current_branch!"
    if errorlevel 1 (
        echo [ERROR] Failed to merge changes. You may need to resolve conflicts manually.
        pause
        exit /b 1
    ) else (
        echo [SUCCESS] Merged changes successfully!
    )
)

:: Pull latest changes from remote
echo [INFO] Pulling latest changes from remote...
git pull origin develop
if errorlevel 1 (
    echo [WARNING] Failed to pull latest changes. This might cause conflicts.
    set /p continue_anyway="Continue anyway? (y/N): "
    if /i not "!continue_anyway!"=="y" (
        echo [INFO] Operation cancelled. Please resolve conflicts manually.
        pause
        exit /b 1
    )
) else (
    echo [SUCCESS] Pulled latest changes successfully!
)

:: Push to remote
echo [INFO] Pushing changes to remote develop branch...
git push origin develop
if errorlevel 1 (
    echo [ERROR] Failed to push changes. Please check your remote configuration and try again.
    pause
    exit /b 1
) else (
    echo [SUCCESS] Changes pushed successfully to develop branch!
    echo [SUCCESS] Your updates are now live on GitHub!
)

:: Clean up (optional)
if not "!current_branch!"=="develop" (
    echo.
    set /p delete_branch="Delete the feature branch '!current_branch!'? (y/N): "
    if /i "!delete_branch!"=="y" (
        echo [INFO] Deleting feature branch...
        git branch -d "!current_branch!"
        if errorlevel 1 (
            echo [WARNING] Failed to delete feature branch. It may have unmerged changes.
        ) else (
            echo [SUCCESS] Feature branch deleted locally.
            echo Note: If you pushed this branch to remote, you may want to delete it there too.
        )
    )
)

echo.
echo [SUCCESS] Update process completed successfully!
echo [INFO] Repository: https://github.com/richard1912/Network-Usage.git
echo [INFO] Branch: develop
echo.
pause
