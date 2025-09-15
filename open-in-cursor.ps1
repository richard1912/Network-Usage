# Open-Cursor.ps1
param(
    [string]$Path = (Get-Location)
)

$cursorPath = "C:\Users\Richard\AppData\Local\Programs\cursor\Cursor.exe"

Start-Process $cursorPath -ArgumentList "`"$Path`""
