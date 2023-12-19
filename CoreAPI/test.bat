dotnet build
cmd /c "start ""Balancer"" powershell.exe -NoExit -Command ""dotnet run --no-build --urls=http://localhost:5033 --Role=Balancer"""
cmd /c "start ""API"" powershell.exe -NoExit -Command ""dotnet run --no-build --urls=http://localhost:5100 --Role=API"""