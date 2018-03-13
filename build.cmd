@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

dotnet restore .\src\SoundFingerprinting.SQL.sln
dotnet test .\src\SoundFingerprinting.SQL.Tests\SoundFingerprinting.SQL.Tests.csproj -c %config%
dotnet pack .\src\SoundFingerprinting.SQL\SoundFingerprinting.SQL.csproj -c %config% -o ..\..\build