using System.IO;
using Microsoft.Win32;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal static class AdditionalTweaks
{
    internal static IEnumerable<Tweak> Build(RegistryTweakFactory f, BackupStore backups)
    {
        const string explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
        const string content = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        Tweak User(string id, string ru, string en, string description, string english, string path, string name, int value, string? os = null)
        {
            var tweak = f.Create(id, ru, description, "Отзывчивость", "Windows", TweakRisk.Moderate, true,
                RegistryHive.CurrentUser, path, name, value, RegistryValueKind.DWord);
            tweak.EnglishTitle = en;
            tweak.EnglishDescription = english;
            tweak.WindowsProfile = os;
            return tweak;
        }
        yield return User("game-config", "Отключить захват в GameConfigStore", "Disable capture in GameConfigStore",
            "Отключает пользовательский флаг записи игрового видео. Полезно при активном захвате.", "Turns off the user's game recording flag. Relevant when capture is active.",
            @"System\GameConfigStore", "GameDVR_Enabled", 0);
        yield return User("taskbar-motion", "Отключить анимацию панели задач", "Disable taskbar animations",
            "Убирает анимированные переходы панели задач после повторного входа.", "Turns off taskbar transitions after signing in again.", explorer + @"\Advanced", "TaskbarAnimations", 0);
        yield return User("tips", "Отключить советы Windows", "Disable Windows tips",
            "Скрывает подсказки при знакомстве с Windows.", "Hides introductory Windows tips.", content, "SoftLandingEnabled", 0);
        yield return User("start-suggestions", "Скрыть рекомендации в Пуск", "Hide Start suggestions",
            "Отключает предложения приложений в классическом меню Windows 10.", "Disables suggested apps in the Windows 10 Start menu.", content, "SubscribedContent-338388Enabled", 0, "10");
        yield return User("uwp10", "Ограничить фоновые UWP для пользователя", "Restrict background UWP apps",
            "Отключает фоновую активность UWP в Windows 10. Может задержать уведомления.", "Disables background UWP activity on Windows 10. Notifications may be delayed.",
            @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1, "10");
        yield return User("widgets11", "Скрыть виджеты с панели задач", "Hide taskbar widgets",
            "Скрывает кнопку виджетов Windows 11. Производительность зависит от использования виджетов.", "Hides the Windows 11 widgets entry point.", explorer + @"\Advanced", "TaskbarDa", 0, "11");
        yield return User("search-highlights", "Скрыть интересное в поиске", "Disable search highlights",
            "Убирает тематический контент из панели поиска.", "Removes featured content from Windows Search.",
            @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);
        yield return User("startup-delay", "Снять задержку автозагрузки Explorer", "Remove Explorer startup delay",
            "Explorer раньше запускает программы автозагрузки. В начале сеанса нагрузка может вырасти.", "Explorer launches startup apps sooner. Load may rise immediately after sign-in.",
            explorer + @"\Serialize", "StartupDelayInMSec", 0);
        yield return User("aero-peek", "Отключить Aero Peek", "Disable Aero Peek",
            "Отключает предварительный просмотр рабочего стола при наведении.", "Disables the desktop peek preview on hover.",
            @"Software\Microsoft\Windows\DWM", "EnableAeroPeek", 0);
        yield return User("controller-bar", "Убрать вызов Game Bar с геймпада", "Disable controller Game Bar shortcut",
            "Кнопка контроллера больше не открывает игровой оверлей.", "Prevents the controller shortcut from opening the gaming overlay.",
            @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0);
        const string profile = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        var advanced = new (string Id,string Ru,string En,string Detail,string English,string Path,string Name,object Value)[]
        {
            ("mmcss-reserve","Резерв CPU для фоновых задач 10%","10% CPU reserve for background tasks",
                "MMCSS гарантирует низкоприоритетным задачам 10% CPU при конкуренции за процессор.",
                "MMCSS guarantees 10% of CPU resources to lower-priority tasks during contention.",profile,"SystemResponsiveness",10),
            ("mmcss-games","Категория High для потоков MMCSS Games","High category for MMCSS Games threads",
                "Меняет категорию потоков, которые игра зарегистрировала в MMCSS Games. Может мешать обработке звука.",
                "Changes the class of threads registered under MMCSS Games. Audio processing may suffer.",profile + @"\Tasks\Games","Scheduling Category","High"),
            ("mmcss-priority","Приоритет 6 для MMCSS Games","Priority 6 for MMCSS Games",
                "Задаёт базовый приоритет зарегистрированных игровых потоков. В категории High Windows трактует это значение как 2.",
                "Sets priority for registered gaming threads. Windows treats it as 2 when the category is High.",profile + @"\Tasks\Games","Priority",6),
            ("cpu-quantum","Квант планировщика для активного окна","Foreground scheduling quantum",
                "Задаёт Win32PrioritySeparation 0x26 для сравнения отзывчивости активного приложения. Меняет планирование всей системы.",
                "Sets Win32PrioritySeparation to 0x26 for foreground workload comparisons. Affects system-wide scheduling.",
                @"SYSTEM\CurrentControlSet\Control\PriorityControl","Win32PrioritySeparation",38)
        };
        foreach (var a in advanced)
        {
            var tweak = f.Create(a.Id,a.Ru,a.Detail,"Производительность","CPU",TweakRisk.Experimental,true,
                RegistryHive.LocalMachine,a.Path,a.Name,a.Value,a.Value is string ? RegistryValueKind.String : RegistryValueKind.DWord);
            tweak.EnglishTitle=a.En;
            tweak.EnglishDescription=a.English;
            yield return tweak;
        }
        yield return new PowerProfile().Ultimate();
        foreach (var setting in AcPowerSettings.Create(backups)) yield return setting;
        var optionalServices = new (string Name, string Ru, string En)[]
        {
            ("MapsBroker","обновление офлайн-карт","offline map updates"),
            ("Fax","отправка факсов","fax transmission"),
            ("Spooler","печать, включая печать в PDF","printing, including Print to PDF"),
            ("XblAuthManager","вход Xbox Live","Xbox Live sign-in"),
            ("XblGameSave","синхронизация сохранений Xbox","Xbox cloud saves"),
            ("XboxNetApiSvc","сетевые функции Xbox","Xbox networking"),
            ("XboxGipSvc","управление аксессуарами Xbox","Xbox accessory management"),
            ("WMPNetworkSvc","публикация медиатеки по сети","media library sharing"),
            ("RetailDemo","демонстрационный режим магазина","retail demo mode"),
            ("PhoneSvc","телефонная интеграция","phone integration"),
            ("StiSvc","сканеры и камеры WIA","WIA scanners and cameras"),
            ("bthserv","обнаружение и сопряжение устройств Bluetooth","Bluetooth discovery and pairing"),
            ("TrkWks","отслеживание перемещённых ссылок NTFS","tracking moved NTFS link targets"),
            ("SensrSvc","автоматическая регулировка яркости по датчику","adaptive brightness using a sensor"),
            ("SensorService","данные датчиков для приложений","sensor data for apps"),
            ("TabletInputService","сенсорная клавиатура и рукописный ввод","touch keyboard and handwriting"),
            ("wisvc","участие в программе предварительных сборок Windows","Windows Insider preview flights"),
            ("WwanSvc","мобильный интернет WWAN","mobile broadband"),
            ("lfsvc","геолокация приложений","app location"),
            ("CscService","автономные сетевые файлы","offline network files"),
            ("icssvc","мобильная точка доступа","mobile hotspot")
        };
        foreach (var item in optionalServices)
        {
            yield return Stored("svc-" + item.Name, "Отключить запуск " + item.Name, "Disable startup of " + item.Name,
                $"После перезапуска станет недоступна функция «{item.Ru}». Полезно только если служба создаёт измеримую нагрузку.",
                $"Disables {item.En} after restart. Only useful if the service causes measurable load.", "disabled", _ =>
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + item.Name);
                    return Task.FromResult(key?.GetValue("Start") switch {
                        2 => Equals(key.GetValue("DelayedAutoStart"), 1) ? "delayed-auto" : "auto", 3 => "demand", 4 => "disabled",
                        _ => throw new NotSupportedException(item.Name)
                    });
                }, (value,ct) => value is "auto" or "delayed-auto" or "demand" or "disabled"
                    ? SystemTool.Run("sc.exe",ct,"config",item.Name,"start=",value) : throw new InvalidDataException(), backups);
        }
        yield return Stored("high-performance", "Высокая производительность", "High performance",
            "Включает встроенную схему питания при её наличии. Откат возвращает исходный GUID.", "Activates the built-in plan when available. Restore switches back to the saved GUID.",
            "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", async ct =>
            {
                var plans = await SystemTool.Run("powercfg.exe",ct,"/list");
                if (!plans.Contains("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("High performance");
                return (await PowerProfile.Active(ct)).ToString();
            },
            (v, ct) => SystemTool.Run("powercfg.exe", ct, "/setactive", Guid.Parse(v).ToString()), backups);
        foreach (var tweak in DeviceAndTaskTweaks.Create(backups)) yield return tweak;
        yield return Network("tcp-rss", "Масштабирование приёма (RSS)", "Receive-side scaling (RSS)",
            "Распределяет обработку входящих пакетов по ядрам. Требуется поддержка адаптера.", "Distributes receive processing across cores on compatible adapters.",
            "(Get-NetOffloadGlobalSetting).ReceiveSideScaling", "rss", "enabled",
            new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) { ["Enabled"]="enabled", ["Disabled"]="disabled" }, backups);
        yield return Network("tcp-autotuning", "Нормальное окно TCP", "Normal TCP receive window",
            "Разрешает TCP увеличивать окно приёма под пропускную способность соединения.", "Lets TCP grow the receive window to use available bandwidth.",
            "(Get-NetTCPSetting -SettingName Internet).AutoTuningLevelLocal", "autotuninglevel", "normal",
            new[] { "disabled","highlyrestricted","restricted","normal","experimental" }.ToDictionary(x=>x,x=>x,StringComparer.OrdinalIgnoreCase), backups);

        foreach (var service in new[] { "SysMain", "DiagTrack", "WSearch", "PcaSvc" })
        {
            string path = @"SYSTEM\CurrentControlSet\Services\" + service;
            yield return new Tweak
            {
                Id = "rp-" + service, Title = service + " · проверка RP", EnglishTitle = service + " · RP check",
                Description = "Majestic / GTA5RP: по присланной памятке службу нельзя отключать или удалять. Зелёный — служба установлена и её запуск разрешён.",
                EnglishDescription = "Majestic / GTA5RP: the supplied guide flags disabling or removing this service. Green means installed and startup allowed.",
                Category = "GTA V", Risk = TweakRisk.Moderate, Impact = "RP", Hardware = "Majestic / GTA5RP",
                Mechanism = @"HKLM\" + path + @"\Start", ReadOnly = true,
                Check = _ =>
                {
                    using var key = Registry.LocalMachine.OpenSubKey(path);
                    return Task.FromResult(key?.GetValue("Start") is int start && start is >= 0 and < 4 ? TweakState.Applied : TweakState.NotApplied);
                },
                Apply = _ => throw new NotSupportedException(), Restore = _ => throw new NotSupportedException()
            };
        }
    }

    private static Tweak Network(string id, string title, string en, string description, string english,
        string query, string option, string desired, Dictionary<string,string> values, BackupStore store)
    {
        var tweak = Stored(id, title, en, description, english, desired, async ct =>
        {
            string value = await SystemTool.Run(@"WindowsPowerShell\v1.0\powershell.exe", ct, "-NoProfile","-NonInteractive","-Command","$ErrorActionPreference='Stop';" + query);
            return values.TryGetValue(value, out var parsed) ? parsed : throw new InvalidDataException(value);
        }, (v,ct) => values.ContainsValue(v) ? SystemTool.Run("netsh.exe",ct,"int","tcp","set","global",option+"="+v) : throw new InvalidDataException(), store, "Сеть");
        tweak.Mechanism = "netsh int tcp set global " + option + "=" + desired;
        if (id == "tcp-rss") tweak.Hardware = "RSS NIC";
        return tweak;
    }

    internal static Tweak Stored(string id, string title, string en, string desc, string english,
        string desired, Func<CancellationToken,Task<string>> read, Func<string,CancellationToken,Task> write,
        BackupStore store, string category = "Производительность", bool requiresAdmin = true) => new()
    {
        Id=id, Title=title, EnglishTitle=en, Description=desc, EnglishDescription=english, Category=category,
        Impact=category, Risk=TweakRisk.Moderate, Mechanism=id.StartsWith("svc-") ? $"sc config {id[4..]} start= {desired}" : desired,
        RequiresAdmin=requiresAdmin,
        RequiresRestart=id.StartsWith("svc-") || id.StartsWith("nic-"), HasBackup=() => store.Contains(id),
        Check=async ct => await read(ct) == desired ? TweakState.Applied : TweakState.NotApplied,
        Apply=async ct =>
        {
            var original = await read(ct);
            if (original == desired) return;
            store.Save(id, original, "String", true);
            await write(desired,ct);
        },
        Restore=async ct =>
        {
            var original = store.Get(id)?.Value ?? throw new InvalidOperationException(UiLanguage.Text("Нет сохранённого значения.", "No saved value."));
            await write(original,ct);
            if (await read(ct) != original) throw new IOException(id);
            store.Forget(id);
        }
    };
}
