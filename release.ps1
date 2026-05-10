# Configurações
$projectName = "MyPlayer"
$projectPath = ".\MyPlayer\MyPlayer.csproj"
$outputDir = ".\MyPlayer\bin\Release\net10.0-windows"

Write-Host "Iniciando processo de release para $projectName..." -ForegroundColor Cyan

# Executando dotnet publish
Write-Host "Executando dotnet publish..." -ForegroundColor Green
dotnet restore --source https://api.nuget.org/v3/index.json --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json
dotnet publish $projectPath -c Release -o $outputDir --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRelease concluído com sucesso!" -ForegroundColor Green
    Write-Host "Os arquivos estão disponíveis em: $(Get-Item $outputDir).FullName" -ForegroundColor Gray
    explorer.exe $outputDir
} else {
    Write-Error "Erro durante o processo de publicação."
}
