<p align="center">
  <img src="src/MagnumTweaker/Assets/Brand.png" width="360" alt="Magnum Tweaker">
</p>

# Magnum Tweaker

**Portable Windows 10/11 x64 performance and gaming settings manager**

[Русский](#русский) · [English](#english) · [Releases](../../releases) · [Contributor guide](AGENTS.md)

## Русский

Magnum Tweaker проверяет настройки производительности Windows и позволяет применять и откатывать поддерживаемые изменения. Приложение написано на C#, .NET 8 и WPF.

- **Каталог.** 86 базовых карточек для каждого профиля Windows. Сетевые карты могут добавлять настройки в зависимости от драйвера. Есть поиск, фильтры по состоянию и риску, сортировка и раскрываемые описания.
- **Magnum pack.** Применяет оставшиеся доступные настройки набора за одно действие. В набор входят игровые и сетевые параметры, настройки отзывчивости и 14 параметров питания от сети. Профиль батареи сохраняется. Повышенное энергопотребление может увеличить нагрев и шум.
- **GTA V.** В базовый каталог входят четыре проверки служб Windows, отмеченных в памятке Majestic / GTA5RP. Эти карточки работают в режиме чтения.
- **Справочник.** 236 записей. Некоторые открывают соответствующую карточку или страницу параметров Windows. Большая часть исходного материала на русском.
- **Интерфейс.** Русский и английский языки, профили Windows 10/11, сворачиваемая панель категорий и общая регулировка громкости.

### Как пользоваться

1. Скачайте портативную сборку из [Releases](../../releases). Распакуйте архив, если скачали ZIP, и запустите `Magnum Tweaker.exe`.
2. Выберите язык и профиль Windows, примите условия использования и дождитесь стартовой проверки. Зелёный статус означает, что значение совпадает, красный — что доступная настройка ещё не применена, серый — что она недоступна, оранжевый — что проверка завершилась ошибкой.
3. Выберите твик или Magnum pack. Перед первым применением можно создать точку восстановления прямо в приложении или продолжить без неё. Для некоторых изменений Windows запросит права администратора.
4. Кнопка **Откат** появляется на карточке, если Magnum сохранил исходное значение. Настройки интерфейса и данные для отката хранятся в `%LOCALAPPDATA%\MagnumTweaker`.

Один автономный EXE предназначен для Windows 10 и 11 на x64. Доступность отдельных твиков зависит от версии и редакции Windows, драйверов и оборудования. Статус подтверждает значение настройки. FPS, задержка ввода и пинг оцениваются отдельными замерами в игре. [Поддерживаемые версии Windows для .NET](https://learn.microsoft.com/dotnet/core/install/windows#supported-versions)

### Сборка и тесты

Откройте [Magnum-Tweaker.sln](Magnum-Tweaker.sln) в Visual Studio или VS Code с C# Dev Kit. Команды ниже запускаются из корня репозитория в Windows с .NET 8 SDK.

```powershell
.\pipelines\Build.ps1
.\pipelines\Run-Test.ps1
```

Готовый EXE находится в `dist\Magnum Tweaker.exe`, отчёт тестов — в `artifacts\test-results\MagnumTweaker.trx`. Для запуска готового EXE отдельный .NET Runtime не требуется. GitHub Actions запускает **Build** и **Run Test** на push, pull request и вручную.

## English

Magnum Tweaker is a C#, .NET 8, and WPF desktop app for checking, applying, and restoring supported Windows performance settings.

- **Catalog.** 86 base cards per Windows profile. Network adapters may expose additional settings depending on the driver. Search, status and risk filters, sorting, and expandable descriptions help you find each setting.
- **Magnum pack.** Applies the remaining available preset changes in one action. It groups gaming, networking, responsiveness, and 14 AC power settings. Battery settings stay as configured. Higher power draw may increase heat and fan noise.
- **GTA V.** The base catalog includes four read-only checks for Windows services flagged in the Majestic / GTA5RP reference material.
- **Reference guide.** 236 entries. Some open the corresponding catalog card or Windows settings page. Most source material is in Russian.
- **Interface.** Russian and English, Windows 10/11 profiles, a collapsible sidebar, and one volume control for interface sounds.

### Getting started

1. Download the portable build from [Releases](../../releases). Extract the ZIP if you downloaded an archive, then launch `Magnum Tweaker.exe`.
2. Select a language and Windows profile, accept the terms, and wait for the startup scan. Green means the value matches, red means an available change is pending, gray means unavailable, and orange means the check failed.
3. Select a tweak or Magnum pack. Before applying the first change, you can create a restore point in the app or continue without one. Windows may request administrator permission for some changes.
4. A card offers **Restore** when Magnum has a snapshot of the original value. Preferences and rollback data are stored in `%LOCALAPPDATA%\MagnumTweaker`.

One self-contained x64 EXE targets Windows 10 and 11. Individual tweaks depend on the Windows release and edition, drivers, and hardware. The status verifies the setting value. FPS, input latency, and ping require separate in-game measurements. [Windows versions supported by .NET](https://learn.microsoft.com/dotnet/core/install/windows#supported-versions)

### Build and test

Open [Magnum-Tweaker.sln](Magnum-Tweaker.sln) in Visual Studio or VS Code with C# Dev Kit. Run these commands from the repository root on Windows with the .NET 8 SDK.

```powershell
.\pipelines\Build.ps1
.\pipelines\Run-Test.ps1
```

The portable EXE is written to `dist\Magnum Tweaker.exe` and the test report to `artifacts\test-results\MagnumTweaker.trx`. The published EXE includes the .NET Runtime. GitHub Actions runs **Build** and **Run Test** on pushes, pull requests, and manual dispatch.

## Структура репозитория / Repository layout

| Path | Назначение / Purpose |
| --- | --- |
| [src/MagnumTweaker](src/MagnumTweaker) | Приложение, манифест и общие стили / WPF app, manifest, and shared styles |
| [UI](src/MagnumTweaker/UI) | Окна, элементы управления и локализация / Windows, controls, and localization |
| [Models](src/MagnumTweaker/Models), [Services](src/MagnumTweaker/Services) | Каталог, наборы, настройки Windows и откат / Catalog, presets, Windows operations, and rollback |
| [Assets](src/MagnumTweaker/Assets) | Логотип, иконка, шрифты и справочник / Logo, icon, fonts, and reference data |
| [tests](tests) | Тесты xUnit / xUnit tests |
| [pipelines](pipelines), [.github/workflows](.github/workflows) | Сборка и проверки / Build and test automation |

Правила работы с репозиторием / Contribution rules in [AGENTS.md](AGENTS.md).

Шрифты / Fonts: [Inter](src/MagnumTweaker/Assets/Fonts/Inter-OFL.txt) · [Manrope](src/MagnumTweaker/Assets/Fonts/Manrope-OFL.txt).
