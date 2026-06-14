#!/bin/bash
set -euo pipefail

# Configurações
projectName="MyPlayer"
projectPath="./MyPlayer/MyPlayer.csproj"
outputDir="./MyPlayer/bin/Release/net10.0"

echo -e "\e[36mIniciando processo de release para $projectName...\e[0m"

# Finalizando processos ativos para evitar lock de arquivo
echo -e "\e[90mVerificando se $projectName está em execução...\e[0m"
# Mata apenas o processo do binário compilado, evitando matar o shell/script
pkill -f "${outputDir}/" 2>/dev/null || true

# Executando dotnet publish
echo -e "\e[32mExecutando dotnet publish...\e[0m"
dotnet restore "$projectPath" --source https://api.nuget.org/v3/index.json --source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet10/nuget/v3/index.json /p:EnableWindowsTargeting=true
dotnet publish "$projectPath" -c Release -o "$outputDir" --self-contained false /p:EnableWindowsTargeting=true

if [ $? -eq 0 ]; then
    echo -e "\n\e[32mRelease concluído com sucesso!\e[0m"
    echo -e "\e[90mOs arquivos estão disponíveis em: $(realpath "$outputDir")\e[0m"
    xdg-open "$outputDir" 2>/dev/null || true
else
    echo -e "\e[31mErro durante o processo de publicação.\e[0m" >&2
fi
