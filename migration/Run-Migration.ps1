param([switch]$StartApp)

$ErrorActionPreference = 'Stop'
$migrationRoot = Split-Path -Parent $PSScriptRoot
Push-Location $migrationRoot
try {
    Write-Host 'Bật Neo4j trước. Migration dùng kết nối và database trong cấu hình ứng dụng.'
    dotnet run --project Nosql-Neo4j.csproj --launch-profile https -- --migrate
    if ($LASTEXITCODE -ne 0) {
        throw 'Migration thất bại. Kiểm tra Neo4j, cấu hình kết nối và kết quả phía trên trước khi chạy lại.'
    }
    if ($StartApp) {
        dotnet run --project Nosql-Neo4j.csproj --launch-profile https
        if ($LASTEXITCODE -ne 0) { throw 'Ứng dụng khởi động thất bại.' }
    }
} finally {
    Pop-Location
}
