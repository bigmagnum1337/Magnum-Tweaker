# Материалы и проверка Magnum Tweaker

В окне «Справочник оптимизаций» доступны все 217 пронумерованных записей TXT и 19 записей об остальных источниках. Поиск работает по названию, описанию и источнику. Для части пунктов есть переход к соответствующей карточке или к странице параметров Windows. Материалы присланного справочника приведены на русском языке.

## Что найдено в файлах

| Источник | Что меняет | Реализация в Magnum |
| --- | --- | --- |
| windows_10_11_tweaks_implementation_ru.txt | Питание, захват игр, графика, мышь, сеть, службы и задания, AppX, накопители, планировщик, драйверы и BIOS. Среди пунктов есть противоположные варианты EPP, парковки и CPU Boost, а также измерения и действия в меню игры. | Карточки для проверяемых изменений, справочник для всех 217 пунктов. Значения питания читаются у активной схемы для работы от сети. |
| Useful_PingFix.7z | Два SPG для TCP Optimizer меняют окно приёма, RSS, параметры разгрузки, TTL, MTU и ряд параметров TCP и MMCSS. Есть значения для конкретного GUID адаптера. Вложены сторонние сетевые утилиты и BAT перезагрузки. | Глобальные RSS и автонастройка TCP, отдельные параметры для адаптеров, которые объявили нужное значение в драйвере. Автонастройка normal соответствует пункту 69 TXT. Отключение автонастройки из SPG отражено в справочнике. |
| USEFUL FREE OPTIMIZATION v2 (RU).zip | BAT копирует Ultimate Performance. Сводный REG меняет параметры памяти, планировщика, питания и графики. Отдельные REG меняют службы Xbox. Есть ссылки на драйверы, загрузчик внешнего деблоатера, MSI Utility, AdwCleaner, NVIDIA Profile Inspector и профиль GTA V. | Выбор и откат плана, параметры MMCSS, отдельные службы Xbox и графические настройки GTA V Legacy. |
| Useful Settings.nip внутри ZIP | В Base Profile 71 запись. Есть настройки VRR, VSync, фильтрации, кэша, очереди кадров, питания, SLI и Optimus. Preferred OpenGL GPU содержит ID устройства автора. | Описание профиля в справочнике. Массовый импорт затронул бы глобальные настройки драйвера. |
| GTA 5 legacy/settings.xml внутри ZIP | Профиль задаёт в том числе 1280×1024 при 240 Гц, низкую детализацию, отключённые MSAA, FXAA и глубину резкости. | Одна карточка устанавливает отдельно присланный пресет Legacy (1920 × 1080, 239 Гц). Название видеокарты берётся из текущего файла. Откат восстанавливает исходный XML целиком. |
| Еженедельный ZIP, Очистка кеша.bat | Удаляет временные файлы, Prefetch, журналы Windows и содержимое SoftwareDistribution. Стирание журналов лишает диагностики, а Windows затем может заново загружать данные. | В справочнике описано действие и есть переход к управлению хранилищем Windows, где пользователь выбирает категории. |
| YUKEE-FPSBOOST.apbx | В конфигурации есть питание, мышь, захват, прозрачность, приоритеты, NTFS, индексирование и службы, удаление приложений и системных компонентов. Отдельные разделы устанавливают ПО, меняют оформление, защиту и активацию. Внутри есть скрипты восстановления, поэтому разные части могут задавать противоположные значения. | Отдельные действия для питания, планировщика, Game DVR, мыши, интерфейса и служб. Плейбук и его вложенные программы изучены как данные. |
| AME Beta.exe | По метаданным это AME Wizard Beta 0.8.4 от Ameliorated. Конкретные изменения описывает выбранный плейбук. | Исполняемый файл не требуется для работы Magnum. |
| Скриншот Ultimate Performance | Команда дублирует шаблон, затем пользователь выбирает созданную схему. | Magnum сохраняет GUID предыдущей схемы, создаёт свою копию и активирует её. Откат удаляет только копию, созданную приложением. |
| Скриншот со службами | Памятка для GTA 5 RP предупреждает об отключении или удалении SysMain, DiagTrack, WSearch и PcaSvc. В protected-services.yml плейбука первые три переводятся в автоматический запуск, PcaSvc в ручной. | Четыре карточки проверяют наличие службы и разрешение запуска. Пометка Majestic / GTA5RP отражает присланную памятку и запрос автора. Официальное правило обоих серверов по этим четырём службам не установлено. |

## CrystalCry

Публичное описание охватывает схему питания, HAGS, эффекты интерфейса, энергосбережение NVIDIA, планировщик CPU, аппаратные таймеры, службы, очистку и Recall. Для части этих задач Magnum имеет собственную реализацию. Публичные страницы не раскрывают полный список ключей, значений или закрытые алгоритмы. [Описание CrystalCry](https://crystalcry.org/en/) и [поддержка](https://crystalcry.org/en/support).

## Как читать результат

Зелёный и красный обозначают совпадение и расхождение проверяемого параметра. Серый означает отсутствие поддерживаемой настройки, оранжевый означает ошибку чтения. Отдельные карточки RP показывают разрешение запуска службы. Запись настройки драйвера или HAGS и её фактическая работа во время игры — разные измерения. Для сравнения игрового результата нужны повторные прогоны одной сцены с одинаковыми условиями, замер времени кадра и 1% low, а для сети — один сервер с замерами RTT, джиттера и потерь.

## Основные источники

- Windows gaming settings: <https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11>
- Windowed game optimizations: <https://support.microsoft.com/windows/hardware/display-graphics/optimizations-for-windowed-games-in-windows-11>
- TCP performance and RSS: <https://learn.microsoft.com/troubleshoot/windows-server/networking/tcpip-performance-known-issues>
- Network adapter tuning: <https://learn.microsoft.com/windows-server/networking/technologies/network-subsystem/net-sub-performance-tuning-nics>
- Delivery Optimization modes: <https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-reference>
- Powercfg: <https://learn.microsoft.com/windows-hardware/design/device-experiences/powercfg-command-line-options>
- Processor power management: <https://learn.microsoft.com/windows-hardware/customize/power-settings/configure-processor-power-management-options>
- QoS and user presence: <https://learn.microsoft.com/windows/win32/procthread/quality-of-service>
- BCDEdit debug flags: <https://learn.microsoft.com/windows-hardware/drivers/devtest/bcdedit--set>
- Timer resolution guidance: <https://learn.microsoft.com/windows/win32/multimedia/timer-resolution>
- MMCSS registry behavior: <https://learn.microsoft.com/windows/win32/procthread/multimedia-class-scheduler-service>
