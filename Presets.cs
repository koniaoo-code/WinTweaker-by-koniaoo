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
            "gamedvr_off", "xbox_gamebar_off", "mouse_accel_off", "fso_off",
            "net_throttle_off", "games_priority", "power_throttle_off", "ndu_off", "ultimate_power",
        },
        ["privacy"] = new[]
        {
            "tel_main", "activity", "diagtrack", "location", "adv_id", "cortana", "wer",
            "feedback", "priv_clip", "priv_apps", "start_ads", "lock_ads", "bing",
            "speech_off", "find_device_off", "cloud_content_off", "priv_account_info",
            "priv_contacts", "priv_diagnostics", "autorun_off", "llmnr_off", "remote_assist_off",
            "settings_suggest_off", "finish_setup_off", "welcome_exp_off", "explorer_ads_off",
        },
        ["minimal"] = new[]
        {
            "visual_fx", "superfetch", "search_idx", "transparency",
            "hide_search", "hide_widgets", "tips", "auto_inst", "start_ads",
            "menu_delay", "startup_delay_off", "settings_suggest_off", "finish_setup_off", "explorer_ads_off",
        },
    };
}
