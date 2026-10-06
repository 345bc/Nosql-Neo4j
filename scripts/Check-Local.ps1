param([int]$Port = 7281)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Push-Location $taskRoot
$taskServer = $null
try {
    dotnet build Nosql-Neo4j.csproj -p:UseAppHost=false -o obj/local-check
    if ($LASTEXITCODE -ne 0) { throw 'Build thất bại.' }
    dotnet run --project Tests/PracticeChecks/PracticeChecks.csproj -p:UseAppHost=false -p:BaseOutputPath=bin/verification/
    if ($LASTEXITCODE -ne 0) { throw 'Service checks thất bại.' }
    $taskDll = Join-Path $taskRoot 'obj/local-check/Nosql-Neo4j.dll'
    $taskServer = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @(
        ('"' + $taskDll + '"'), '--urls', "https://localhost:$Port", '--environment', 'Development',
        '--Logging:EventLog:LogLevel:Default', 'None') -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $taskRoot 'obj/local-check/server.log') `
        -RedirectStandardError (Join-Path $taskRoot 'obj/local-check/server-error.log')
    $taskReady = $false
    for ($taskRetry = 0; $taskRetry -lt 20; $taskRetry++) {
        if ($taskServer.HasExited) { throw 'Web kiểm thử không khởi động. Xem obj/local-check/server-error.log.' }
        try {
            $taskResponse = Invoke-WebRequest "https://localhost:$Port/Account/Login" -SkipCertificateCheck
            $taskReady = $taskResponse.StatusCode -eq 200
        } catch { Start-Sleep -Milliseconds 500 }
        if ($taskReady) { break }
    }
    if (!$taskReady) { throw 'Web kiểm thử chưa sẵn sàng.' }
    dotnet run --project Tests/Neo4jChecks/Neo4jChecks.csproj -p:UseAppHost=false -p:BaseOutputPath=bin/verification/ -- --run-local --base-url "https://localhost:$Port"
    if ($LASTEXITCODE -ne 0) { throw 'Neo4j/HTTP checks thất bại.' }
    dotnet $taskDll --environment Development --Logging:EventLog:LogLevel:Default None --verify-data
    if ($LASTEXITCODE -ne 0) { throw 'Kiểm tra dữ liệu thất bại.' }
} finally {
    if ($taskServer -and !$taskServer.HasExited) { Stop-Process -Id $taskServer.Id }
    Pop-Location
}
