return {
    Title = "TaiwuCommunityTranslation",
    Version = 1,
    Author = "Taiwu Mods Community",
    Description = "Taiwu Community Translation",
    FrontendPlugins = {"TaiwuCommunityTranslation.dll"},
    DefaultSettings = {
        {
			Key = "enableAutoSizing",
			DisplayName = "Auto-Sizing",
			SettingType = "Toggle",
			DefaultValue = true,
		},
        {
			Key="fontMin",
			DisplayName="Font-Min",
			SettingType="Slider",
			DefaultValue=16,
			MinValue=16,
			MaxValue=32
		},
        {
			Key="fontMax",
			DisplayName="Font-Max",
			SettingType="Slider",
			DefaultValue=24,
			MinValue=16,
			MaxValue=32
		}
    }
}
