using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArchEmperorLib;

public class ModManager : MonoBehaviour
{
	private struct ModWindows
	{
		public string guid;
		public Button entryBtn;
		// public GameObject root;
		public GameObject credits;
		public GameObject settings;
	}

	private static bool ready = false;
	private static Canvas mainCanvas;
	private static Dictionary<string, ModWindows> modWindows = [];
	private static GameObject modEntriesContainer;
	private static GameObject modCreditsRoot;
	private static GameObject modSettingsRoot;
	private static GameObject modCreditsContainer;
	private static GameObject modSettingsContainer;
	private static Button settingsBtn;
	private static Button creditsBtn;
	private static Button settingsCancelBtn;
	private static Button settingsApplyBtn;
	private static string currentActiveGUID = "";

	public delegate void ModSettignsCancel(string guid);
	public static event ModSettignsCancel OnModSettignsCancel;
	public delegate void ModSettignsApply(string guid);
	public static event ModSettignsApply OnModSettignsApply;
	public delegate void ModSettignsRefresh(string guid);
	public static event ModSettignsApply OnModSettignsRefresh;

	public static void Initialize()
	{
		GameObject canvasObj = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("ModCanvas_MainMenu"));
		if (!canvasObj) throw new Exception("Couldn't load MainMenu asset from bundle.");

		mainCanvas = canvasObj.GetComponent<Canvas>();
		SceneManager.MoveGameObjectToScene(canvasObj, SceneManager.GetSceneByName("MainMenu"));
		canvasObj.active = false;
		DontDestroyOnLoad(canvasObj);

		SetupCanvas();

		ready = true;
	}

	public static bool SetCredits(string guid, CreditBlock creditBlock)
	{
		if (!ready) return false;
		if (creditBlock == null)
			throw new ArgumentNullException(nameof(creditBlock));
		if (modWindows.ContainsKey(guid) && modWindows[guid].credits != null)
			throw new Exception($"Mod {guid} already registered credits.");

		GameObject creditsObject = ModCreditsManager.GenerateBlock(guid, creditBlock);
		if (creditsObject == null) return false;
		return SetCredits(guid, creditsObject);
	}
	public static bool SetCredits(string guid, GameObject creditsObject)
	{
		if (!ready) return false;
		if (creditsObject == null)
			throw new ArgumentNullException(nameof(creditsObject));
		if (modWindows.ContainsKey(guid) && modWindows[guid].credits != null)
			throw new Exception($"Mod {guid} already registered credits.");

		ModCreditsManager.AddBundlesIfAny(guid, creditsObject);

		creditsObject.transform.SetParent(modCreditsContainer.transform);
		creditsObject.active = false;
		Utils.RescaleUI(creditsObject);

		modWindows.TryGetValue(guid, out var windows);
		windows.credits = creditsObject;
		modWindows[guid] = windows;

		return true;
	}
	public static bool SetSettings(string guid, SettingBlock settingBlock)
	{
		if (!ready) return false;
		if (settingBlock == null)
			throw new ArgumentNullException(nameof(settingBlock));
		if (modWindows.ContainsKey(guid) && modWindows[guid].settings != null)
			throw new Exception($"Mod {guid} already registered settings.");

		GameObject settingsObject = ModSettingsManager.GenerateBlock(guid, settingBlock);
		if (settingsObject == null) return false;
		return SetSettings(guid, settingsObject);
	}
	public static bool SetSettings(string guid, GameObject settingsObject)
	{
		if (!ready) return false;
		if (settingsObject == null)
			throw new ArgumentNullException(nameof(settingsObject));
		if (modWindows.ContainsKey(guid) && modWindows[guid].settings != null)
			throw new Exception($"Mod {guid} already registered settings.");

		ModCreditsManager.AddBundlesIfAny(guid, settingsObject);

		settingsObject.transform.SetParent(modSettingsContainer.transform);
		settingsObject.active = false;
		Utils.RescaleUI(settingsObject);

		modWindows.TryGetValue(guid, out var windows);
		windows.settings = settingsObject;
		modWindows[guid] = windows;

		return true;
	}

	public static void SelectMod(string guid = null)
	{
		if (!ready) return;
		foreach (var modWindow in modWindows.Values)
		{
			modWindow.settings?.active = false;
			modWindow.credits?.active = false;
		}

		guid ??= MyPluginInfo.PLUGIN_GUID;
		currentActiveGUID = guid;
		if (!modWindows.ContainsKey(currentActiveGUID)) currentActiveGUID = MyPluginInfo.PLUGIN_GUID;
		ModWindows window = modWindows[guid];

		settingsBtn.gameObject.active = window.settings != null;
		creditsBtn.gameObject.active = window.credits != null;

		window.credits?.active = true;
		window.settings?.active = true;

		if (window.settings != null) ViewSettings();
		else ViewCredits();
	}
	private static void ViewCredits()
	{
		modCreditsRoot.active = true;
		modSettingsRoot.active = false;
		// ModWindows window = modWindows[currentActiveGUID];
		// window.credits?.active = true;
		// window.settings?.active = false;
	}
	private static void ViewSettings()
	{
		modCreditsRoot.active = false;
		modSettingsRoot.active = true;
		ModWindows window = modWindows[currentActiveGUID];
		// window.credits?.active = false;
		// window.settings?.active = true;
		OnModSettignsRefresh.Invoke(currentActiveGUID);
	}

	#region UI
	public static void OpenModScreen()
	{
		// Scene_MainMenu mainMenu = GameObject.FindFirstObjectByType<Scene_MainMenu>();
		// mainMenu?.MainMenuGp.interactable = false;
		// EventManager.ins.inGamePause?.pauseUI.
		mainCanvas.gameObject.active = true;

		GrabFocus();
		SelectMod();
	}
	public static void CloseModScreen()
	{
		Scene_MainMenu mainMenu = GameObject.FindFirstObjectByType<Scene_MainMenu>();
		mainMenu?.MainMenuGp.interactable = true;
		mainCanvas.gameObject.active = false;

		EventSystem.current.SetSelectedGameObject(mainMenu?.NavButtons[0].gameObject, null);
	}
	public static void GrabFocus() => EventSystem.current.SetSelectedGameObject(modEntriesContainer.transform.GetChild(0).gameObject, null);
	public static void RescaleUI()
	{
		Utils.RescaleUI(mainCanvas?.gameObject);
		// for (int i = 0; i < modEntriesContainer.transform.childCount; i++)
		// {
		// 	Utils.RescaleUI(modEntriesContainer.transform.GetChild(i).gameObject);
		// }
	}
	#endregion

	#region SETUP
	private static void SetupCanvas()
	{
		modEntriesContainer = mainCanvas.transform.Find("HBox").Find("Left").Find("Mod list").Find("Viewport").Find("Content").gameObject;
		modCreditsRoot = mainCanvas.transform.Find("HBox").Find("Right").Find("Credits").gameObject;
		modSettingsRoot = mainCanvas.transform.Find("HBox").Find("Right").Find("Settings").gameObject;
		modCreditsContainer = modCreditsRoot.transform.Find("ModContent").Find("Viewport").Find("Content").gameObject;
		modSettingsContainer = modSettingsRoot.transform.Find("ModContent").Find("Viewport").Find("Content").gameObject;
		settingsBtn = mainCanvas.transform.Find("HBox").Find("Right").Find("Nav").Find("SettingsBtn").GetComponent<Button>();
		creditsBtn = mainCanvas.transform.Find("HBox").Find("Right").Find("Nav").Find("CreditsBtn").GetComponent<Button>();
		settingsCancelBtn = mainCanvas.transform.Find("HBox").Find("Right").Find("Settings").Find("Buttons").Find("Cancel").GetComponent<Button>();
		settingsApplyBtn = mainCanvas.transform.Find("HBox").Find("Right").Find("Settings").Find("Buttons").Find("Apply").GetComponent<Button>();

		foreach (var modRec in ModRegistry.LocalMods)
		{
			Button entryButton = GenerateButton(modRec.Id, modRec.Name, modRec.Version);

			var modWins = new ModWindows() { guid = modRec.Id, entryBtn = entryButton };
			modWindows.Add(modRec.Id, modWins);
		}

		creditsBtn.onClick.AddListener((Action)(() => ViewCredits()));
		settingsBtn.onClick.AddListener((Action)(() => ViewSettings()));
		settingsCancelBtn.onClick.AddListener((Action)(() => OnModSettignsCancel.Invoke(currentActiveGUID)));
		settingsApplyBtn.onClick.AddListener((Action)(() => OnModSettignsApply.Invoke(currentActiveGUID)));

		Transform pauseMenu = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "Canvas_System").transform.Find("Pause").Find("Container").Find("inner");
		GameObject newPauseButton = GameObject.Instantiate(pauseMenu.GetChild(0).gameObject);
		newPauseButton.GetComponent<Button>().onClick.RemoveAllListeners();
		newPauseButton.GetComponent<Button>().onClick.m_PersistentCalls.Clear();
		newPauseButton.GetComponent<Button>().onClick.AddListener((Action)(() => OpenModScreen()));
		newPauseButton.transform.GetChild(0).GetComponent<LocalizeStringEvent>().enabled = false;
		newPauseButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Mod Settings";
		newPauseButton.transform.SetParent(pauseMenu.transform);
		newPauseButton.transform.SetSiblingIndex(2);
	}
	private static Button GenerateButton(string guid, string name, string version)
	{
		GameObject entryButton = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("ModEntryButton"));
		entryButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
		entryButton.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = version;
		entryButton.transform.SetParent(modEntriesContainer.transform);
		entryButton.GetComponent<Button>().onClick.AddListener((Action)(() => SelectMod(guid)));
		Utils.RescaleUI(entryButton);
		return entryButton.GetComponent<Button>();
	}
	#endregion
}