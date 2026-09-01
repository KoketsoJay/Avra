$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSCommandPath

$services = @(
    @{ Name = 'ProductService';   Port = 5001; Db = 'AvraProductService' },
    @{ Name = 'CartService';      Port = 5002; Db = 'AvraCartService' },
    @{ Name = 'UserService';      Port = 5003; Db = 'AvraUserService' },
    @{ Name = 'EventsService';    Port = 5004; Db = 'AvraEventsService' },
    @{ Name = 'CommunityService'; Port = 5005; Db = 'AvraCommunityService' },
    @{ Name = 'WellnessService';  Port = 5006; Db = 'AvraWellnessService' }
)

$csprojTemplate = @'
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\Avra.ServiceDefaults\Avra.ServiceDefaults.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
'@

foreach ($svc in $services) {
    $dir = Join-Path $root $svc.Name
    Set-Content -Path (Join-Path $dir "$($svc.Name).csproj") -Value $csprojTemplate -Encoding UTF8

    $settings = [ordered]@{
        Urls            = "http://localhost:$($svc.Port)"
        Logging         = [ordered]@{ LogLevel = [ordered]@{ Default = 'Information'; 'Microsoft.EntityFrameworkCore' = 'Warning' } }
        AllowedHosts    = 'localhost'
        Cors            = [ordered]@{ AllowedOrigins = @('http://localhost:5173', 'http://localhost:5199') }
        RateLimiting    = [ordered]@{ PermitsPerMinute = 300; AuthPermitsPerMinute = 10 }
        Jwt             = [ordered]@{ Issuer = 'avra-auth-service'; Audience = 'avra-api'; AccessTokenMinutes = 60 }
        ConnectionStrings = [ordered]@{ DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=$($svc.Db);Trusted_Connection=true;Encrypt=false;" }
    }
    $settings | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $dir 'appsettings.json') -Encoding UTF8

    # Development-only signing key. Production supplies Jwt__SigningKey from the secret store.
    $dev = [ordered]@{
        Jwt = [ordered]@{ SigningKey = 'avra-local-development-signing-key-do-not-use-in-production' }
        Serilog = [ordered]@{ MinimumLevel = 'Information' }
    }
    $dev | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $dir 'appsettings.Development.json') -Encoding UTF8

    Write-Host "rewrote $($svc.Name)"
}
