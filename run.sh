#!/bin/bash
cd /mnt/disco_d/projetos/c_sharp/players/MyPlayer/MyPlayer
dotnet restore
dotnet build
dotnet run

# obs: precisa de sudo pacman -S vlc