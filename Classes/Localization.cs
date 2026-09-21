using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ArchEmperorLib;

public class Localization
{
	public enum Locales { ENGLISH, JAPANESE, CHINESE, CHINESE_SIMPLIFIED, DEFAULT }


	private static Dictionary<string, Dictionary<Locales, Dictionary<string, string>>> modTranslations = new();

	public delegate void LocaleChanged(Config.LanguageType lang);
	public static event LocaleChanged OnLocaleChanged;

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(TranslationList), nameof(TranslationList.ChangeLanguage))]
	public static void SetLocale(UnityEngine.Localization.Locale locale) // Config.LanguageType lang
	{
		Config.LanguageType lang = Config.LanguageType.English;
		if (locale.Identifier.Code == LocalizationSettings.AvailableLocales.Locales[0].Identifier.Code) lang = Config.LanguageType.Chinese_s;
		else if (locale.Identifier.Code == LocalizationSettings.AvailableLocales.Locales[1].Identifier.Code) lang = Config.LanguageType.Chinese_t;
		else if (locale.Identifier.Code == LocalizationSettings.AvailableLocales.Locales[3].Identifier.Code) lang = Config.LanguageType.Japanese;

		OnLocaleChanged?.Invoke(lang);
	}

	public static string GetText(string guid, string item)
	{
		Locales locale = Config.Language switch
		{
			Config.LanguageType.Japanese => Locales.JAPANESE,
			Config.LanguageType.Chinese_s => Locales.CHINESE_SIMPLIFIED,
			Config.LanguageType.Chinese_t => Locales.CHINESE,
			_ => Locales.ENGLISH,
		};
		return GetTextLocale(guid, item, locale);
	}

	public static string GetTextLocale(string guid, string item, Locales locale)
	{
		Dictionary<string, string> dict = modTranslations[guid][locale];
		/* locale switch
		{
			Locales.JAPANESE => modTranslations[guid][Locales.JAPANESE],
			Locales.CHINESE => modTranslations[guid][Locales.CHINESE],
			Locales.CHINESE_SIMPLIFIED => modTranslations[guid][Locales.CHINESE_SIMPLIFIED],
			Locales.ENGLISH => modTranslations[guid][Locales.ENGLISH],
			_ => modTranslations[guid][Locales.DEFAULT],
		};*/

		bool ok = dict.TryGetValue(item, out string str);
		// Return found item.
		if (ok) return str;
		// Else, try again with the default set.
		if (locale != Locales.DEFAULT)
			return GetTextLocale(guid, item, Locales.DEFAULT);
		// Else, since the default is English and nothing was found, return an error.
		return "ERR: No Text";
	}

	public static bool AddTranslation(string guid, Dictionary<Locales, Dictionary<string, string>> dict)
	{
		if (modTranslations.ContainsKey(guid)) return false;
		if (!dict.ContainsKey(Locales.ENGLISH)) dict[Locales.ENGLISH] = [];
		if (!dict.ContainsKey(Locales.JAPANESE)) dict[Locales.JAPANESE] = [];
		if (!dict.ContainsKey(Locales.CHINESE)) dict[Locales.CHINESE] = [];
		if (!dict.ContainsKey(Locales.CHINESE_SIMPLIFIED)) dict[Locales.CHINESE_SIMPLIFIED] = [];
		if (!dict.ContainsKey(Locales.DEFAULT)) dict[Locales.DEFAULT] = [];
		modTranslations.Add(guid, dict);
		return true;
	}

	public static class FontSwitcher
	{
		public struct FontAsset
		{
			public TMP_FontAsset font;
			public Material material;
		}
		private static Dictionary<Config.LanguageType, FontAsset> assets = [];
		private static bool hasInit = false;
		public static void Init()
		{
			if (hasInit) return;
			assets = new() {
			{ Config.LanguageType.English, new() { font = Plugin.assets.LoadAsset<TMP_FontAsset>("Mulish-Regular SDF")} },//, material = Plugin.assets.LoadAsset<Material>("")
			{ Config.LanguageType.Japanese, new() { font = Plugin.assets.LoadAsset<TMP_FontAsset>("NotoSansJP-Regular SDF")}},
			{ Config.LanguageType.Chinese_s, new() { font = Plugin.assets.LoadAsset<TMP_FontAsset>("NotoSansSC-Regular SDF")}},
			{ Config.LanguageType.Chinese_t, new() { font = Plugin.assets.LoadAsset<TMP_FontAsset>("NotoSansTC-Regular SDF")}}
			};
			hasInit = true;
		}

		public static void HookText(TextMeshProUGUI tmp)
		{
			tmp?.font = assets[Config.Language].font;
			OnLocaleChanged += (lang) =>
			{
				tmp?.font = assets[lang].font;
				// tmp?.fontMaterial = assets[Config.Language].material;
			};
		}
	}
}