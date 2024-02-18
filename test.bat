dotnet build CoreAPI/CoreAPI.csproj
cmd /c "start ""API"" powershell.exe -NoExit -Command ""dotnet run --project CoreAPI/CoreAPI.csproj --no-build --urls=http://localhost:5100 --Role=API"""
cmd /c "start ""API"" powershell.exe -NoExit -Command ""dotnet run --project CoreAPI/CoreAPI.csproj --no-build --urls=http://localhost:5101 --Role=API"""
cmd /c "start ""Balancer"" powershell.exe -NoExit -Command ""dotnet run --project CoreAPI/CoreAPI.csproj --no-build --urls=https://localhost:5033 --Role=Balancer"""
