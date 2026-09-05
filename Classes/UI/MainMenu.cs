using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace ArchEmperorLib.UI
{
	public class MainMenuPatch
	{
		private static class TranslationKeys { public const string OFF = "OFF", ON = "ON", LOW = "LOW", MEDIUM = "MEDIUM", HIGH = "HIGH", RACE_ONLY = "RACE_ONLY", DIORAMA_ONLY = "DIORAMA_ONLY", NONE = "NONE", REDUCED = "REDUCED", FULL = "FULL", DEFAULT = "DEFAULT"; };
		private static readonly Dictionary<Localization.Locales, Dictionary<string, string>> translations = new()
		{
			{Localization.Locales.DEFAULT, new()
				{
					{TranslationKeys.OFF, "Off"},
					{TranslationKeys.ON, "On"},
					{TranslationKeys.LOW, "Low"},
					{TranslationKeys.MEDIUM, "Medium"},
					{TranslationKeys.HIGH, "High"},
					{TranslationKeys.RACE_ONLY, "Only in race"},
					{TranslationKeys.DIORAMA_ONLY, "Diorama only"},
					{TranslationKeys.NONE, "None"},
					{TranslationKeys.REDUCED, "Reduced"},
					{TranslationKeys.FULL, "Full"},
					{TranslationKeys.DEFAULT, "Default"},
				}
			},
			{Localization.Locales.JAPANESE, new()
				{
					{TranslationKeys.RACE_ONLY, "レース中のみ"},
					{TranslationKeys.DIORAMA_ONLY, "ジオラマのみ"},
					{TranslationKeys.NONE, "なし"},
					{TranslationKeys.REDUCED, "少ない"},
					{TranslationKeys.FULL, "最大"},
					{TranslationKeys.DEFAULT, "デフォルト"},
				}
			},
			{Localization.Locales.CHINESE, new()
				{
					{TranslationKeys.RACE_ONLY, "僅限賽事"},
					{TranslationKeys.NONE, "無"},
					{TranslationKeys.REDUCED, "降價"},
					{TranslationKeys.FULL, "完整"},
					{TranslationKeys.DEFAULT, "預設"},
				}
			},
			{Localization.Locales.CHINESE_SIMPLIFIED, new()
				{
					{TranslationKeys.RACE_ONLY, "仅限比赛"},
					{TranslationKeys.NONE, "无"},
					{TranslationKeys.REDUCED, "简化"},
					{TranslationKeys.FULL, "完整"},
					{TranslationKeys.DEFAULT, "默认"},
				}
			},
		};
		private static readonly Dictionary<Enum, string> settingRelations = new()
		{
			{ModSettingsManager.ToggleEnum.off, TranslationKeys.OFF},
			{ModSettingsManager.ToggleEnum.on, TranslationKeys.ON},

			{ModSettingsManager.TieredEnum.high, TranslationKeys.HIGH},
			{ModSettingsManager.TieredEnum.medium, TranslationKeys.MEDIUM},
			{ModSettingsManager.TieredEnum.low, TranslationKeys.LOW},

			{ModSettingsManager.TieredWithOffEnum.high, TranslationKeys.HIGH},
			{ModSettingsManager.TieredWithOffEnum.medium, TranslationKeys.MEDIUM},
			{ModSettingsManager.TieredWithOffEnum.low, TranslationKeys.LOW},
			{ModSettingsManager.TieredWithOffEnum.off, TranslationKeys.OFF},

			{ModSettingsManager.QuantityEnum.full, TranslationKeys.FULL},
			{ModSettingsManager.QuantityEnum.reduced, TranslationKeys.REDUCED},
			{ModSettingsManager.QuantityEnum.none, TranslationKeys.NONE},

			{ModSettingsManager.RaceOnlyEnum.off, TranslationKeys.OFF},
			{ModSettingsManager.RaceOnlyEnum.raceOnly, TranslationKeys.RACE_ONLY},
			{ModSettingsManager.RaceOnlyEnum.on, TranslationKeys.ON},

			{ModSettingsManager.DioramaOnlyEnum.off, TranslationKeys.OFF},
			{ModSettingsManager.DioramaOnlyEnum.dioramaOnly, TranslationKeys.DIORAMA_ONLY},
			{ModSettingsManager.DioramaOnlyEnum.on, TranslationKeys.ON},
		};

		private static GameObject introSignature;
		private static Button mainModButton;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Scene_GameInit), nameof(Scene_GameInit.Start))]
		public static void InitializeGame()
		{
			Config.Debug_OfflineLeaderBoards = false;

			Localization.FontSwitcher.Init();
			
			Cursor.lockState = CursorLockMode.Confined;
			
			LocalizationSettings.add_SelectedLocaleChanged((Il2CppSystem.Action<UnityEngine.Localization.Locale>)((locale) => Localization.SetLocale(locale)));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
		public static void InitializeMain(ref Scene_MainMenu __instance)
		{
			TextMeshProUGUI sampledText = __instance.MainMenuRoot.transform.Find("StartMenu").GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
			UIUtils.Fonts.mainFont = sampledText.font;
			UIUtils.Fonts.mainFontMat = sampledText.fontSharedMaterial;

			ModManager.Initialize();

			GameObject creditBlock = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("ArchEmperorCredits"));
			ModManager.SetCredits(MyPluginInfo.PLUGIN_GUID, creditBlock);

			Localization.AddTranslation(MyPluginInfo.PLUGIN_GUID, translations);
			ModSettingsManager.AddTextRelations(MyPluginInfo.PLUGIN_GUID, settingRelations);

			AddModSignature(ref __instance);
			AddModButton(ref __instance);

			// Mod ready emit()
		}

		public static void AddModSignature(ref Scene_MainMenu __instance)
		{
			Transform introMenu = __instance.MainMenuRoot.transform.Find("StartMenu");

			GameObject signatureObj = new("Mod signature");
			signatureObj.transform.SetParent(introMenu);

			RectTransform rect = signatureObj.AddComponent<RectTransform>();
			rect.pivot = new(0, 0);
			rect.anchorMin = new(0, 0);
			rect.anchorMax = new(0, 0);
			rect.sizeDelta = new(250, 20);
			rect.anchoredPosition = new(20, 0);

			TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
			signature.name = "Mod Signature";
			signature.text = "ArchEmperor " + MyPluginInfo.PLUGIN_VERSION;
			signature.fontSize = 16;
			signature.font = UIUtils.Fonts.mainFont;
			signature.fontMaterial = UIUtils.Fonts.mainFontMat;
			signature.transform.localScale = Vector3.one;

			introSignature = signatureObj;
		}

		public static void AddModButton(ref Scene_MainMenu __instance)
		{
			Transform mainMenu = __instance.MainMenuRoot.transform.Find("MainMenu_All");

			GameObject buttonObj = new("Mod Button");
			buttonObj.transform.SetParent(mainMenu);

			RectTransform modRect = buttonObj.AddComponent<RectTransform>();
			modRect.pivot = new(0, 0);
			modRect.anchorMin = new(0, 0);
			modRect.anchorMax = new(0, 0);
			modRect.sizeDelta = new(260, 40);
			modRect.anchoredPosition = new(20, 4);
			Image btnImage = buttonObj.AddComponent<Image>();
			btnImage.color = new(1, 1, 1, 1);
			Button button = buttonObj.AddComponent<Button>();
			button.targetGraphic = btnImage;
			button.transition = Selectable.Transition.ColorTint;
			ColorBlock colorBlock = new()
			{
				normalColor = new Color(0, 0, 0, 1),
				highlightedColor = new Color(.3f, .3f, .3f, 1),
				pressedColor = new Color(.4f, .6f, .7f, 1),
				selectedColor = new Color(0, 0, 0, 1),
				disabledColor = new Color(0, 0, 0, 1),
				colorMultiplier = 1,
				fadeDuration = .15f
			};
			button.colors = colorBlock;
			button.onClick.AddListener((Action)(() => { ModManager.OpenModScreen(); }));

			// Add logo
			GameObject logoObj = new("ModLogo");
			logoObj.transform.SetParent(buttonObj.transform);
			RectTransform logoRect = logoObj.AddComponent<RectTransform>();
			logoRect.pivot = new(0, 0);
			logoRect.anchorMin = new(0, 0);
			logoRect.anchorMax = new(0, 0);
			logoRect.sizeDelta = new(36, 36);
			logoRect.anchoredPosition = new(2, 2);
			Sprite modLogo = Plugin.assets.LoadAsset<Sprite>("ArchEmperorLogoMini");
			Image logoImg = logoObj.AddComponent<Image>();
			logoImg.sprite = modLogo;

			// Add text
			GameObject signatureObj = new("Mod signature");
			signatureObj.transform.SetParent(buttonObj.transform);
			RectTransform rect = signatureObj.AddComponent<RectTransform>();
			rect.pivot = new(0, 0);
			rect.anchorMin = new(0, 0);
			rect.anchorMax = new(0, 0);
			rect.sizeDelta = new(250, 26);
			rect.anchoredPosition = new(50, 0);
			TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
			signature.name = "Mod Signature";
			signature.text = "ArchEmperor " + MyPluginInfo.PLUGIN_VERSION;
			signature.fontSize = 16f;
			signature.font = UIUtils.Fonts.mainFont;
			signature.fontMaterial = UIUtils.Fonts.mainFontMat;
			signature.transform.localScale = Vector3.one;

			mainModButton = button;
			Utils.SetUINavigation(__instance.NavButtons[__instance.NavButtons.Count - 1], Utils.NavDirEnum.DOWN, mainModButton);
			Utils.SetUINavigation(mainModButton, Utils.NavDirEnum.UP, __instance.NavButtons[__instance.NavButtons.Count - 1]);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Update))]
		public static void MainMenuUpdate()
		{
			if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ModManager.CloseModScreen();
			if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) ModManager.CloseModScreen();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.SetResolution))]
		public static void RescaleUI()
		{
			Utils.RescaleUI(introSignature);
			Utils.RescaleUI(mainModButton?.gameObject);
			ModManager.RescaleUI();
		}
	}
}