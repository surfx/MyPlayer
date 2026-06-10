# Configurações
$projectName = "MyPlayer"
$projectPath = ".\MyPlayer\MyPlayer.csproj"
$outputDir = ".\MyPlayer\bin\Release\net10.0"

Write-Host "Iniciando processo de release para $projectName..." -ForegroundColor Cyan

# Finalizando processos ativos para evitar lock de arquivo
Write-Host "Verificando se $projectName está em execução..." -ForegroundColor Gray
Get-Process $projectName -ErrorAction SilentlyContinue | Stop-Process -Force

# Executando dotnet publish
Write-Host "Executando dotnet publish..." -ForegroundColor Green
dotnet restore $projectPath --source https://api.nuget.org/v3/index.json --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json
dotnet publish $projectPath -c Release -o $outputDir --self-contained false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRelease concluído com sucesso!" -ForegroundColor Green
    Write-Host "Os arquivos estão disponíveis em: $(Get-Item $outputDir).FullName" -ForegroundColor Gray
    explorer.exe $outputDir
} else {
    Write-Error "Erro durante o processo de publicação."
}
