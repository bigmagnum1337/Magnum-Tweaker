<p align="center">
  <img src="Assets/Brand.png" width="360" alt="Magnum Tweaker">
</p>

# Magnum Tweaker

**Portable Windows 10/11 x64 performance and gaming settings manager** · **Портативный менеджер настроек Windows 10/11 x64**

[Русский](#русский) · [English](#english) · [Releases](../../releases) · [Contributor guide](AGENTS.md)

## Русский

Magnum Tweaker — приложение на C#, .NET 8 и WPF. Оно проверяет настройки Windows и игр, показывает текущее состояние и позволяет менять поддерживаемые параметры с откатом.

### Возможности

| Раздел | Что доступно |
| --- | --- |
| Каталог | **86 базовых карточек** для каждого профиля Windows, включая четыре проверки служб RP. Дополнительные сетевые карточки зависят от обнаруженных свойств адаптера. |
| Категории | FPS и игры, задержка ввода, производительность, сеть, отзывчивость, готовые настройки и GTA V. Панель категорий сворачивается до значков. |
| Готовые настройки | Игровой набор без разгона, включая 14 параметров повышенного энергопотребления от сети в активной схеме. Настройки от батареи сохраняются. Возможны дополнительный нагрев, шум и расход электроэнергии. Счётчики показывают, сколько изменений уже применено, доступно и недоступно. |
| Справочник | **236 записей**, включая 217 исходных пунктов и материалы по приложенным пакетам. У части записей есть переход к карточке или параметрам Windows. |
| Интерфейс | Русский и английский языки, выбор профиля Windows 10/11, тёмное оформление, поиск и фильтры, раскрываемые описания, единый ползунок громкости. |

### Как пользоваться

1. Запустите портативный EXE, выберите язык и профиль Windows. При первом запуске прочтите пользовательское соглашение.
2. Дождитесь обязательной проверки. Она читает параметры без применения твиков. Зелёный статус показывает совпадение значения, красный — доступную настройку с другим значением, серый — недоступность, оранжевый — ошибку проверки.
3. Выберите настройку или готовый набор. Для части системных изменений требуются права администратора. Если приложение сохранило исходное значение, в карточке будет доступен откат.

Настройки интерфейса и снимки для отката хранятся в `%LOCALAPPDATA%\MagnumTweaker` отдельно от EXE. Доступность зависит от версии и редакции Windows, драйверов и установленных приложений. Статус подтверждает значение параметра. FPS, пинг и время кадра проверяются отдельно в игре. Пометки Majestic / GTA5RP следуют исходной памятке, приведённой в разборе материалов.

### Совместимость

Один автономный EXE для Windows 10 и 11 на x64. Поддержка конкретной сборки Windows зависит от .NET 8 и жизненного цикла Windows. Смена профиля в приложении управляет каталогом, а применимость определяется фактической системой. [Таблица Microsoft](https://learn.microsoft.com/dotnet/core/install/windows#supported-versions)

### Сборка и проверка

Запустите эти команды из корня репозитория в Windows с .NET 8 SDK.

```powershell
.\pipelines\Build.ps1
.\pipelines\Run-Test.ps1
```

Сборка создаёт один автономный файл `dist\Magnum Tweaker.exe`. Для запуска этого файла отдельный .NET Runtime не нужен. Проверки **Build** и **Run Test** также запускаются в GitHub Actions на push, pull request и вручную.

## English

Magnum Tweaker is a C#, .NET 8, and WPF desktop tool for inspecting, applying, and restoring supported Windows and game settings.

### Features

| Area | What you get |
| --- | --- |
| Catalog | **86 base cards** per Windows profile, including four read-only RP service checks. Additional network cards depend on detected adapter capabilities. |
| Categories | FPS and gaming, input lag, performance, network, responsiveness, presets, and GTA V. The sidebar collapses to icons. |
| Presets | A gaming preset with 14 higher-power AC settings in the active plan and counts for applied, available, and unavailable tweaks. It uses stock boost, leaves battery settings alone, and may increase heat, fan noise, and energy use. |
| Reference guide | **236 entries**, including 217 numbered source items and notes on the supplied optimization packs. Selected entries link to matching cards or Windows settings. Much of the source material is in Russian. |
| Interface | Russian and English UI, Windows 10/11 profile selector, dark theme, search and filters, expandable descriptions, and a shared volume slider. |

### Using the app

1. Launch the portable EXE, select a language and Windows profile, and read the terms on first launch.
2. Wait for the mandatory scan. It reads the current settings without applying tweaks. Green means the value matches, red means an available setting has a different value, gray means unavailable, and orange means the check failed.
3. Apply an individual tweak or a preset. Some changes require administrator rights. A card offers Restore when the application has a snapshot.

Interface preferences and rollback snapshots live in `%LOCALAPPDATA%\MagnumTweaker`, separate from the EXE. Availability depends on the Windows build and edition, drivers, and installed applications. Status checks verify settings. FPS, latency, and frame times require in-game measurements. Majestic / GTA5RP labels reflect the supplied guide referenced in the research notes.

### Compatibility

The same self-contained x64 EXE targets Windows 10 and 11. Support for a specific Windows build depends on .NET 8 and the Windows lifecycle. The in-app profile filters the catalog, and the actual host determines tweak eligibility. [Microsoft support matrix](https://learn.microsoft.com/dotnet/core/install/windows#supported-versions)

### Build and test

Run these commands from the repository root on Windows with the .NET 8 SDK.

```powershell
.\pipelines\Build.ps1
.\pipelines\Run-Test.ps1
```

The Build pipeline produces one self-contained executable at `dist\Magnum Tweaker.exe`. The executable runs without a separately installed .NET Runtime. GitHub Actions runs **Build** and **Run Test** on pushes, pull requests, and manual dispatch.

### Repository map

| Path | Purpose |
| --- | --- |
| `App.xaml`, `*Window.xaml` | Shared WPF design resources and window layouts |
| `Models/`, `Services/` | Tweak states, catalog, system access, backups, and presets |
| `Assets/` | Embedded artwork and reference material |
| `tests/` | xUnit tests for core behavior and bundled resources |
| `pipelines/`, `.github/workflows/` | Local pipeline entry points and GitHub Actions workflows |

See [AGENTS.md](AGENTS.md) for branch naming, review expectations, and the application-wide design rules.

Typography / Шрифты: [Inter](https://rsms.me/inter/) and [Manrope](https://fonts.google.com/specimen/Manrope). [Inter license](Assets/Fonts/Inter-OFL.txt) · [Manrope license](Assets/Fonts/Manrope-OFL.txt)
