using System.Runtime.InteropServices;
using System.ComponentModel;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal static class AcPowerSettings
{
    [DllImport("powrprof.dll")]
    private static extern uint PowerReadACValueIndex(IntPtr root, in Guid plan, in Guid group, in Guid setting, out uint value);
    [DllImport("powrprof.dll")]
    private static extern uint PowerWriteACValueIndex(IntPtr root, in Guid plan, in Guid group, in Guid setting, uint value);

    public static IEnumerable<Tweak> Create(BackupStore backup)
    {
        const string cpu = "54533251-82be-4824-96c1-47b60b740d00";
        var options = new (string Group,string Setting,uint Value,string Ru,string En,string Hardware)[]
        {
            (cpu,"bc5038f7-23e0-4960-96da-33abaf5935ec",100,"Максимум CPU 100%","Maximum CPU state 100%","CPU"),
            (cpu,"893dee8e-2bef-41e0-89c6-b55d0929964c",100,"Минимум CPU 100%","Minimum CPU state 100%","CPU"),
            (cpu,"36687f9e-e3a5-4dbf-b1dc-15eb381c6863",0,"EPP: приоритет производительности","EPP: prefer performance","CPU · EPP"),
            (cpu,"36687f9e-e3a5-4dbf-b1dc-15eb381c6863",25,"EPP: компромисс 25%","EPP: balanced bias 25%","CPU · EPP"),
            (cpu,"0cc5b647-c1df-4637-891a-dec35c318583",100,"Оставлять все ядра распаркованными","Keep all cores unparked","CPU · PPM"),
            (cpu,"0cc5b647-c1df-4637-891a-dec35c318583",50,"Держать минимум 50% ядер активными","Keep at least 50% of cores unparked","CPU · PPM"),
            (cpu,"be337238-0d82-4146-a960-4f3749d470c7",2,"Агрессивное ускорение CPU","Aggressive CPU boost","CPU · Boost"),
            (cpu,"be337238-0d82-4146-a960-4f3749d470c7",4,"Эффективное агрессивное ускорение CPU","Efficient aggressive CPU boost","CPU · Boost"),
            ("501a4d13-42af-4429-9fd1-a8218c268e20","ee12f906-d277-404b-b6da-e5fa1a576df5",0,"Отключить энергосбережение линии PCIe","Disable PCIe link power saving","PCIe"),
            ("2a737441-1930-4402-8d77-b2bebba308a3","48e6b7a6-50f5-4782-a5d4-53bb8f07e226",0,"Отключить выборочную приостановку USB","Disable USB selective suspend","USB"),
            ("0012ee47-9041-4b5d-9b77-535fba8b1442","6738e2c4-e8a5-4a42-b16a-e040e769756e",0,"Не останавливать диск при простое","Keep disks spinning when idle","HDD"),
            ("238c9fa8-0aad-41ed-83f4-97be242c8f20","29f6c1db-86da-48c5-9fdb-f2b67b1f44da",0,"Не переводить ПК в сон по таймеру","Disable automatic sleep timeout","AC"),
            ("7516b95f-f776-4464-8c53-06167f40cc99","3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e",0,"Не выключать дисплей по таймеру","Disable display idle timeout","Display")
        };
        foreach (var option in options)
        {
            var group = Guid.Parse(option.Group);
            var setting = Guid.Parse(option.Setting);
            uint Read(Guid plan)
            {
                uint error = PowerReadACValueIndex(IntPtr.Zero, plan, group, setting, out var value);
                if (error is 2 or 50 or 1168) throw new NotSupportedException();
                if (error != 0) throw new Win32Exception((int)error);
                return value;
            }
            async Task Write(Guid plan,uint value,CancellationToken ct)
            {
                uint error = PowerWriteACValueIndex(IntPtr.Zero, plan, group, setting, value);
                if (error != 0) throw new Win32Exception((int)error);
                if (await PowerProfile.Active(ct) == plan)
                    await SystemTool.Run("powercfg.exe",ct,"/setactive",plan.ToString());
                if (Read(plan) != value) throw new InvalidOperationException(setting.ToString());
            }
            yield return new Tweak
            {
                Id=$"ac-{setting}-{option.Value}", Category="Производительность", Title=option.Ru, EnglishTitle=option.En,
                Impact="AC", Hardware=option.Hardware, Risk=TweakRisk.Experimental,
                Description=$"{option.Ru} в активной схеме при питании от сети. Может повысить энергопотребление. Откат действует на выбранную схему.",
                EnglishDescription=$"{option.En} in the active AC plan. Power use may rise. Restore targets the selected plan.",
                Mechanism=$"AC · {group} / {setting} = {option.Value}",
                HasBackup=() =>
                {
                    try { return backup.Contains($"ac:{PowerProfile.ActiveGuid()}:{setting}"); }
                    catch (Win32Exception) { return false; }
                },
                Check=async ct => Read(await PowerProfile.Active(ct)) == option.Value ? TweakState.Applied : TweakState.NotApplied,
                Apply=async ct =>
                {
                    var plan=await PowerProfile.Active(ct);
                    var value=Read(plan);
                    if (value==option.Value) return;
                    backup.Save($"ac:{plan}:{setting}",value,"DWORD",true);
                    await Write(plan,option.Value,ct);
                },
                Restore=async ct =>
                {
                    var plan=await PowerProfile.Active(ct);
                    var key=$"ac:{plan}:{setting}";
                    if (!uint.TryParse(backup.Get(key)?.Value,out var original))
                        throw new InvalidOperationException(UiLanguage.Text("Нет снимка этой схемы.", "No snapshot for this plan."));
                    await Write(plan,original,ct);
                    backup.Forget(key);
                }
            };
        }
    }
}
