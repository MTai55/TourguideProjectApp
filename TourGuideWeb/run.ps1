# run.ps1
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd TourGuideAPI; dotnet run"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd TourismApp.Web; dotnet run"