using System.Windows;
using System.Windows.Input;

namespace MagnumTweaker;

public partial class TermsWindow : Window
{
    internal const int Version = 1;
    private bool _leaving;

    internal TermsWindow(bool requireAcceptance)
    {
        InitializeComponent();
        WindowTransitions.PrepareEntrance(this);
        Title = Heading.Text = UiLanguage.Text("Пользовательское соглашение", "Terms of Use");
        CloseTermsButton.ToolTip = UiLanguage.Text("Закрыть", "Close");
        System.Windows.Automation.AutomationProperties.SetName(CloseTermsButton, (string)CloseTermsButton.ToolTip);
        DeclineButton.Content = requireAcceptance
            ? UiLanguage.Text("Не принимаю", "Decline")
            : UiLanguage.Text("Закрыть", "Close");
        AcceptButton.Content = UiLanguage.Text("Принимаю", "Accept");
        AcceptButton.Visibility = requireAcceptance ? Visibility.Visible : Visibility.Collapsed;
        TermsBody.Text = UiLanguage.Text(RussianTerms, EnglishTerms);
    }

    private async void Accept_Click(object sender, RoutedEventArgs e) => await ExitAsync(true);
    private async void Close_Click(object sender, RoutedEventArgs e) => await ExitAsync(false);

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        await ExitAsync(false);
    }

    private async Task ExitAsync(bool accepted)
    {
        if (_leaving) return;
        _leaving = true;
        await WindowTransitions.FadeOutAsync(this);
        if (IsVisible) DialogResult = accepted;
    }

    private const string RussianTerms = """
        1. Назначение программы
        Magnum Tweaker позволяет проверять и изменять параметры Windows. Применяйте изменения только на устройствах, которыми вы вправе управлять. Решение о применении и откате каждого изменения принимаете вы.

        2. Риски и восстановление
        Изменения могут влиять на стабильность, совместимость, сеть и доступ к данным. Они могут потребовать перезагрузки, а откат отдельной настройки не заменяет полную резервную копию. Перед изменениями сохраните важные данные и убедитесь, что сможете восстановить систему.

        3. Гарантии и ответственность
        В пределах, разрешённых применимым законодательством, программа предоставляется «как есть» без гарантий совместимости, бесперебойной работы или прироста производительности. Пользователь несёт риск последствий выбранных им изменений. Разработчик и правообладатель не отвечают за ущерб, вызванный изменениями, которые пользователь самостоятельно выбрал и подтвердил, включая сбои, потерю данных и простой, в той мере, в какой закон допускает такое ограничение.

        4. Обязательные права
        Соглашение не ограничивает права и ответственность, которые нельзя исключить или ограничить по применимому законодательству.

        Кнопка «Принимаю» подтверждает согласие с этими условиями. При отказе программа закроется без применения твиков.
        """;

    private const string EnglishTerms = """
        1. Purpose
        Magnum Tweaker lets you check and change Windows settings. Make changes only on devices you are authorized to manage. You decide which changes to apply or restore.

        2. Risks and recovery
        Changes may affect stability, compatibility, networking, and access to data. They may require a restart. Restoring individual settings is no substitute for a full backup. Before making changes, back up important data and make sure you can recover your system.

        3. Warranties and liability
        To the extent permitted by applicable law, the software is provided as is, without guarantees of compatibility, uninterrupted operation, or performance improvements. You assume the risks of the changes you choose. To the extent permitted by law, the developer and rights holder are not liable for damage arising from changes you independently chose and confirmed, including system failures, data loss, and downtime.

        4. Mandatory rights
        These terms do not limit any rights or liabilities that applicable law does not allow to be waived or limited.

        Select Accept to agree to these terms. If you decline, the app will close without applying any tweaks.
        """;
}
