namespace MagnumTweaker.Models;

internal static class EnglishTweakText
{
    internal static readonly IReadOnlyDictionary<string, (string Title, string Description)> Entries =
        new Dictionary<string, (string, string)>
        {
            ["game-mode"] = ("Game Mode", "Prioritizes game resources, especially when other apps are active in the background."),
            ["game-dvr"] = ("Disable Game DVR background recording", "Stops continuous background recording of recent gameplay."),
            ["game-dvr-policy"] = ("Disable Windows Game Recording", "Sets a system policy to turn off Game DVR for regular apps."),
            ["hags"] = ("Hardware-accelerated GPU scheduling", "Requests HAGS in Windows. The dot tracks the saved flag. Runtime support depends on the GPU and driver after a restart."),
            ["power-throttling"] = ("Disable Power Throttling", "Prevents Windows from throttling background processes to save power."),
            ["presence-qos"] = ("Keep QoS during inactivity", "Prevents Windows from lowering an app's QoS after a period without input. Useful for benchmarks and controller-based games."),
            ["mouse-accel"] = ("Disable mouse acceleration", "Turns off Enhance pointer precision for more predictable cursor movement."),
            ["mouse-threshold1"] = ("Mouse acceleration threshold I", "Sets the first system mouse acceleration threshold to zero."),
            ["mouse-threshold2"] = ("Mouse acceleration threshold II", "Sets the second system mouse acceleration threshold to zero."),
            ["menu-delay"] = ("Open menus faster", "Reduces the delay before system menus appear."),
            ["window-animations"] = ("Disable window animations", "Removes minimize and maximize animations."),
            ["transparency"] = ("Disable Windows transparency", "Turns off system transparency effects and reduces compositing work slightly."),
            ["visual-effects"] = ("Optimize visual effects for performance", "Selects the Windows profile with minimal visual effects."),
            ["delivery-p2p"] = ("Disable peer-to-peer updates", "Stops sharing updates with other PCs while keeping Microsoft downloads available."),
            ["background-apps"] = ("Restrict Microsoft Store apps", "Prevents UWP apps from running in the background. Some app notifications may stop."),
            ["search-web"] = ("Disable web search in Start", "Removes web queries and results from local search."),
            ["notifications"] = ("Disable toast notifications", "Keeps pop-up notifications from appearing over games."),
            ["high-performance"] = ("High performance power plan", "Activates the built-in high performance plan. Laptops may run hotter and use more battery."),
            ["tcp-rss"] = ("Enable Receive Side Scaling", "Distributes network packet processing across cores on supported Ethernet adapters."),
            ["tcp-autotuning"] = ("Set TCP Auto-Tuning to Normal", "Restores the recommended dynamic TCP window. Disabling it can reduce throughput.")
        };
}
