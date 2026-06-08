namespace WinTweaker.Data;

/// <summary>One-click tweak profiles. Each value is a list of tweak ids to enable.</summary>
public static class Presets
{
    public static readonly Dictionary<string, string[]> Sets = new()
    {
        ["gaming"] = new[]
        {
            "max_power", "game_mode", "hags", "timer_res", "cpu_prio",
            "visual_fx", "fast_startup", "hide_widgets", "nagle", "tcp_auto", "tcp_fast",
        },
        ["privacy"] = new[]
        {
            "tel_main", "activity", "diagtrack", "location", "adv_id", "cortana", "wer",
            "feedback", "priv_clip", "priv_apps", "start_ads", "lock_ads", "bing",
        },
        ["minimal"] = new[]
        {
            "visual_fx", "superfetch", "search_idx", "transparency",
            "hide_search", "hide_widgets", "tips", "auto_inst", "start_ads",
        },
    };
}
