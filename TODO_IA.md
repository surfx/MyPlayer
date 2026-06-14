# STATUS FINAL — Migração WinForms → Avalonia (MyPlayer)

**Compilação: ✅ OK** (0 erros, 12 warnings — apenas nullable e APIs obsoletas)

## O que foi feito

### Projeto convertido para Avalonia
| Arquivo | Ação |
|---|---|
| `MyPlayer.csproj` | Trocado de `net10.0-windows` + `UseWindowsForms` para `net10.0` + pacotes Avalonia/SkiaSharp |
| `Program.cs` | Entry point com `AppBuilder` do Avalonia |
| `App.axaml` / `App.axaml.cs` | Novo — Application com FluentTheme |
| `MainWindow.axaml` | Novo — layout XAML completo (Grid 6 linhas) |
| `MainWindow.axaml.cs` | Toda a lógica da UI adaptada do WinForms |

### Classes de negócio mantidas (inalteradas)
- `PlayerControl.cs` — NAudio continua funcionando no Linux
- `MusicControl.cs` — NAudio + WaveOutEvent
- `PlayList.cs` — JSON com System.Text.Json
- `MusicaDTO.cs` — modelo de dados
- `ControleEstados.cs` — serialização JSON do estado
- `FormularioEstado.cs` / `ListVewState.cs` — estado da UI

### Utilitários adaptados
| Arquivo | Mudança |
|---|---|
| `InvokeAux.cs` | `Dispatcher.UIThread` do Avalonia no lugar de `Control.Invoke` |
| `Util.cs` | Removeu métodos `System.Drawing`; manteve Shuffle, FormatFileSize, MusicPath |
| `ThemeManager.cs` | Removeu DWM/Registry; usa `RequestedThemeVariant` do Avalonia |
| `ListViewAux.cs` | `ObservableCollection<MusicaItem>` no lugar de `ListView` |
| `EstadoFormAux.cs` | Adaptado para novos tipos Avalonia |
| `TreeViewUtil.cs` | `TreeViewItem` do Avalonia no lugar de `TreeNode` |
| `FiltrarMusicas.cs` | `ObservableCollection<MusicaItem>` no lugar de `ListViewItem` |
| `MusicaItem.cs` | Novo — `MusicaDTO` com `IsChecked` para checkboxes |

### Waveform com SkiaSharp (cross-platform)
- `WaveImage.cs` reescrito com `SkiaSharp` (não usa mais `System.Drawing`)
- Renderiza picos de áudio com gradiente
- `GetUpdateImage()` sobrepõe cinza na parte não reproduzida
- `ClickPictureBox()` para seek

### GlobalKeyboardHook
- Removido o código Win32 (`user32.dll`, `SetWindowsHookEx`)
- Virou no-op em Linux
- `MainWindow_KeyDown` no window lida com F3, Media keys (apenas quando janela ativa)

### Limpeza
- ✅ `frmMyPlayer.cs` removido
- ✅ `frmMyPlayer.Designer.cs` removido
- ✅ `frmMyPlayer.resx` removido
- ✅ `Properties/Resources.Designer.cs` removido
- ✅ `Properties/Resources.resx` removido
- ✅ `libs/` removido (todo o `NAudio.WaveFormRenderer` que usava `System.Drawing`)

## Como compilar e executar

```bash
cd /mnt/disco_d/projetos/c_sharp/players/MyPlayer/MyPlayer
dotnet restore
dotnet build
dotnet run
```

## Melhorias futuras (não críticas)

- Migrar `OpenFolderDialog`/`SaveFileDialog`/`OpenFileDialog` para `StorageProvider` API (deprecated mas funcional)
- Implementar contexto de menu na lista (abrir pasta, copiar caminho, deletar) — o código da MainWindow já cria o `ContextMenu` mas ele precisa ser ativado (click direito)
- Adicionar suporte a global keyboard hook no Linux (via `evdev` ou DBus)
- Tratar nullable warnings (CS8600, CS8601, CS8604)
- Atualizar ícones dos botões no dark mode (atualmente usam os mesmos PNGs)
