using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal static class DeviceAndTaskTweaks
{
    private const string NicClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    private static RegistryKey? OpenDriverKey(RegistryKey parent, string name)
    {
        try { return parent.OpenSubKey(name); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { return null; }
    }

    public static IEnumerable<Tweak> Create(BackupStore backups)
    {
        var tasks = new (string Path, string Ru, string En)[]
        {
            (@"Maps\MapsUpdateTask","Обновление офлайн-карт","Offline map updates"),
            (@"Maintenance\WinSAT","Плановая оценка быстродействия WinSAT","Scheduled WinSAT assessment"),
            (@"Customer Experience Improvement Program\Consolidator","Агрегация данных CEIP","CEIP data aggregation"),
            (@"Customer Experience Improvement Program\UsbCeip","Сбор статистики USB CEIP","USB CEIP statistics"),
            (@"Application Experience\ProgramDataUpdater","Сбор данных совместимости программ","Application compatibility data collection"),
            (@"DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector","Сбор диагностических данных дисков","Disk diagnostic data collection")
        };
        foreach (var task in tasks)
        {
            string path = @"\Microsoft\Windows\" + task.Path;
            var tweak = AdditionalTweaks.Stored("task-" + task.Path, task.Ru, task.En,
                $"Отключает задание «{task.Ru}» в планировщике. Оно перестанет запускаться по расписанию.",
                $"Disables the scheduled task for {task.En.ToLowerInvariant()}. Its automatic runs will stop.",
                "off", ct => Task.Run(() => TaskState(path), ct),
                (value, ct) => Task.Run(() => TaskState(path, value), ct), backups);
            tweak.Mechanism = path;
            yield return tweak;
        }

        NetworkInterface[] interfaces;
        try { interfaces = NetworkInterface.GetAllNetworkInterfaces(); }
        catch (NetworkInformationException) { interfaces = []; }
        using var network = OpenDriverKey(Registry.LocalMachine,NicClass);
        if (network == null) yield break;
        foreach (string subkey in network.GetSubKeyNames().Where(s => s.Length == 4 && s.All(char.IsDigit)))
        {
            using var driver = OpenDriverKey(network,subkey);
            if (!Guid.TryParse(driver?.GetValue("NetCfgInstanceId") as string, out var id)) continue;
            var nic = interfaces.FirstOrDefault(n => Guid.TryParse(n.Id, out var g) && g == id);
            if (nic == null) continue;
            var options = new (string Key, int Value, string Ru, string En, string Effect, string EffectEn)[]
            {
                ("*InterruptModeration",0,"Отключить модерацию прерываний","Disable interrupt moderation","Пакеты могут обрабатываться раньше, нагрузка на CPU может вырасти.","Packets may be handled sooner at the cost of more CPU interrupts."),
                ("*EEE",0,"Отключить Energy Efficient Ethernet","Disable Energy Efficient Ethernet","Запрещает переход линии в режим пониженного питания между пакетами.","Prevents the link from entering low-power idle between packets."),
                ("*FlowControl",0,"Отключить Ethernet Flow Control","Disable Ethernet flow control","Отключает кадры PAUSE. При перегрузке потери пакетов могут увеличиться.","Disables PAUSE frames. Packet loss may increase under congestion."),
                ("*RSS",1,"Включить RSS адаптера","Enable adapter RSS","Распределяет приём пакетов по ядрам поддерживаемого CPU.","Distributes receive processing across available processor cores."),
                ("*LsoV2IPv4",0,"Отключить LSO для IPv4","Disable IPv4 LSO","Переносит сегментацию TCP с адаптера на CPU для сравнения поведения драйвера.","Moves TCP segmentation from the adapter to the CPU for driver troubleshooting."),
                ("*LsoV2IPv6",0,"Отключить LSO для IPv6","Disable IPv6 LSO","Переносит сегментацию TCP/IPv6 с адаптера на CPU.","Moves TCP/IPv6 segmentation from the adapter to the CPU."),
                ("*RscIPv4",0,"Отключить объединение сегментов IPv4","Disable IPv4 RSC","Отменяет объединение входящих TCP-сегментов драйвером. Может увеличить нагрузку CPU.","Disables receive segment coalescing in the driver. CPU use may increase."),
                ("*RscIPv6",0,"Отключить объединение сегментов IPv6","Disable IPv6 RSC","Меняет обработку входящих TCP/IPv6-сегментов для сравнения задержки и пропускной способности.","Changes incoming TCP/IPv6 processing to compare latency and throughput.")
            };
            foreach (var option in options)
            {
                using var values = OpenDriverKey(driver!,@"Ndi\Params\" + option.Key + @"\enum");
                var allowed = values?.GetValueNames() ?? [];
                string target = option.Value.ToString();
                if (!allowed.Contains(target) || !allowed.Contains(driver.GetValue(option.Key)?.ToString())) continue;
                string path = NicClass + @"\" + subkey;
                string Read()
                {
                    using var adapter = Registry.LocalMachine.OpenSubKey(path);
                    if (!Guid.TryParse(adapter?.GetValue("NetCfgInstanceId") as string, out var actual) || actual != id)
                        throw new NotSupportedException(nic.Description);
                    string? current = adapter!.GetValue(option.Key)?.ToString();
                    return allowed.Contains(current) ? current! : throw new NotSupportedException(option.Key);
                }
                async Task Write(string value, CancellationToken ct)
                {
                    Read();
                    if (!allowed.Contains(value) || !int.TryParse(value, out int index)) throw new InvalidDataException();
                    // Only a parsed GUID, a catalog keyword and a numeric enum value enter the fixed query.
                    string query = "$ErrorActionPreference='Stop';" +
                        $"$a=@(Get-NetAdapter | Where-Object {{ [guid]$_.InterfaceGuid -eq [guid]'{id:D}' }});" +
                        "if($a.Count -ne 1){throw 'Adapter unavailable'};" +
                        "$p=@(Get-NetAdapterAdvancedProperty -Name ([WildcardPattern]::Escape($a[0].Name)) -AllProperties | " +
                        $"Where-Object {{ $_.RegistryKeyword -eq '{option.Key}' }});" +
                        $"if($p.Count -ne 1){{throw 'Property unavailable'}};$p[0] | Set-NetAdapterAdvancedProperty -RegistryValue '{index}' -NoRestart";
                    await SystemTool.Run(@"WindowsPowerShell\v1.0\powershell.exe",ct,"-NoProfile","-NonInteractive","-Command",query);
                }
                var tweak = AdditionalTweaks.Stored($"nic-{id}-{option.Key}",option.Ru,option.En,
                    option.Effect + " Вступает в силу после перезапуска адаптера или ПК.",
                    option.EffectEn + " Takes effect after restarting the adapter or PC.",
                    target,_ => Task.FromResult(Read()),Write,backups,"Сеть");
                tweak.Hardware = nic.Description;
                tweak.Mechanism = $"{option.Key} = {target}";
                yield return tweak;
            }
        }
    }

    private static string TaskState(string path, string? value = null)
    {
        object? service = null, folder = null, task = null;
        try
        {
            service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service", true)!)!;
            ((dynamic)service).Connect();
            int split = path.LastIndexOf('\\');
            folder = ((dynamic)service).GetFolder(path[..split]);
            task = ((dynamic)folder).GetTask(path[(split + 1)..]);
            if (value != null)
                ((dynamic)task).Enabled = value switch { "on" => true, "off" => false, _ => throw new InvalidDataException() };
            return ((dynamic)task).Enabled ? "on" : "off";
        }
        catch (COMException ex) when (ex.HResult is unchecked((int)0x80070002) or unchecked((int)0x80070003))
        { throw new NotSupportedException(path, ex); }
        catch (FileNotFoundException ex) { throw new NotSupportedException(path, ex); }
        catch (DirectoryNotFoundException ex) { throw new NotSupportedException(path, ex); }
        finally
        {
            foreach (var obj in new[] { task, folder, service })
                if (obj != null && Marshal.IsComObject(obj)) Marshal.ReleaseComObject(obj);
        }
    }
}
