using System.Collections.Generic;
using HarmonyLib;

namespace ArchEmperorLib;

public class Localization
{
	public enum Locales { ENGLISH, JAPANESE, CHINESE, CHINESE_SIMPLIFIED, DEFAULT }


	private static Dictionary<string, Dictionary<Locales, Dictionary<string, string>>> modTranslations = new();

	public delegate void LocaleChanged();
	public static event LocaleChanged OnLocaleChanged;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(TranslationList), nameof(TranslationList.ChangeLanguage))]
	public static void SetLocale() // Config.LanguageType lang
	{
		OnLocaleChanged?.Invoke();
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
}