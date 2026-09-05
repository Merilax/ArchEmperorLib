using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArchEmperorLib;

public class ModSettingsManager
{
	public static Dictionary<string, Dictionary<Enum, string>> valueTextRelations = [];

	public enum ToggleEnum { off, on }
	public enum TieredEnum { low, medium, high }
	public enum TieredWithOffEnum { off, low, medium, high }
	public enum RaceOnlyEnum { off, raceOnly, on }
	public enum DioramaOnlyEnum { off, dioramaOnly, on }
	public enum QuantityEnum { none, reduced, full }

	public class ConfigEnums
	{
		public static readonly IReadOnlyList<ToggleEnum> toggleEnums = [ToggleEnum.off, ToggleEnum.on];
		public static readonly IReadOnlyList<TieredEnum> tieredEnums = [TieredEnum.low, TieredEnum.medium, TieredEnum.high];
		public static readonly IReadOnlyList<TieredWithOffEnum> tieredWithDisabledEnums = [TieredWithOffEnum.off, TieredWithOffEnum.low, TieredWithOffEnum.medium, TieredWithOffEnum.high];
		public static readonly IReadOnlyList<RaceOnlyEnum> raceOnlyEnums = [RaceOnlyEnum.off, RaceOnlyEnum.raceOnly, RaceOnlyEnum.on];
		public static readonly IReadOnlyList<DioramaOnlyEnum> dioramaOnlyEnums = [DioramaOnlyEnum.off, DioramaOnlyEnum.dioramaOnly, DioramaOnlyEnum.on];
		public static readonly IReadOnlyList<QuantityEnum> quantityEnums = [QuantityEnum.none, QuantityEnum.reduced, QuantityEnum.full];
	}

	public static bool AddTextRelations(string guid, Dictionary<Enum, string> dict)
	{
		if (valueTextRelations.ContainsKey(guid)) return false;
		valueTextRelations.Add(guid, dict);
		return true;
	}

	public static GameObject GenerateBlock(string guid, SettingBlock settingBlock)
	{
		try
		{
			GameObject tempSettingsObject = new(guid + "Settings");
			VerticalLayoutGroup blockGroup = tempSettingsObject.AddComponent<VerticalLayoutGroup>();
			blockGroup.padding = new(20, 20, 20, 20);
			blockGroup.spacing = 12;
			blockGroup.childAlignment = TextAnchor.UpperCenter;
			blockGroup.childForceExpandHeight = false;
			blockGroup.childForceExpandWidth = true;
			blockGroup.childControlHeight = true;
			blockGroup.childControlWidth = true;
			LayoutElement layoutElement = tempSettingsObject.AddComponent<LayoutElement>();
			layoutElement.flexibleHeight = 1;
			layoutElement.flexibleWidth = 1;

			foreach (var item in settingBlock.Rows)
			{
				switch (item.Key)
				{
					case SettingBlock.TYPE.SEPARATOR:
						GameObject separator = new("Separator");
						separator.transform.SetParent(tempSettingsObject.transform);
						Image sepImg = separator.AddComponent<Image>();
						sepImg.color = new(.7f, .7f, .7f);
						layoutElement = separator.AddComponent<LayoutElement>();
						layoutElement.minHeight = 6;
						layoutElement.flexibleWidth = 1;
						break;
					case SettingBlock.TYPE.LOGO:
						GameObject logo = new("Logo");
						logo.transform.SetParent(tempSettingsObject.transform);
						layoutElement = logo.AddComponent<LayoutElement>();
						layoutElement.minHeight = 170;
						layoutElement.minWidth = 170;
						layoutElement.preferredHeight = 200;
						layoutElement.preferredWidth = 200;
						Sprite logoSprite = item.Value;
						Image logoImg = logo.AddComponent<Image>();
						logoImg.sprite = logoSprite;
						break;
					case SettingBlock.TYPE.SETTING:
						GameObject row = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("SettingEntrySample"));
						row.transform.SetParent(tempSettingsObject.transform);
						var title = row.transform.Find("Name").GetComponent<TextMeshProUGUI>();
						var value = row.transform.Find("Value").GetComponent<TextMeshProUGUI>();
						var leftBtn = row.transform.Find("Left").GetComponent<Button>();
						var rightBtn = row.transform.Find("Right").GetComponent<Button>();
						Localization.OnLocaleChanged += (lang) => { title?.text = Localization.GetText(guid, item.Value.titleKey); };
						title.text = Localization.GetText(guid, item.Value.titleKey);
						Localization.FontSwitcher.HookText(title);

						item.Value.cycleConfigEntry.SetTextMesh(value);
						leftBtn.onClick.AddListener((System.Action)(() => { item.Value.cycleConfigEntry.OnLeftButton(); }));
						rightBtn.onClick.AddListener((System.Action)(() => { item.Value.cycleConfigEntry.OnRightButton(); }));

						row.active = true;
						break;
				}
			}
			return tempSettingsObject;
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogError(ex);
			return null;
		}
	}
}

public class CycleConfigEntry<T>
{
	private readonly string _translationGUID;
	private readonly ConfigEntry<T> _configEntry;
	private readonly IReadOnlyList<T> _options;
	public TextMeshProUGUI _text;
	private T _pendingValue;
	public T Pending => _pendingValue;
	public T Value => _configEntry.Value;

	public CycleConfigEntry(string translationGUID, ConfigEntry<T> configEntry, IReadOnlyList<T> options, TextMeshProUGUI textMesh = null)
	{
		_translationGUID = translationGUID;
		_configEntry = configEntry;
		_options = options;
		_pendingValue = configEntry.Value;
		_text = textMesh;
		if (_pendingValue is Enum)
		{
			_text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]);
			Localization.OnLocaleChanged += (lang) => { _text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]); };
			Localization.FontSwitcher.HookText(_text);
		}
	}

	private int GetPendingIndex()
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].Equals(_pendingValue))
				return i;
		}
		return 0;
	}
	public void SetTextMesh(TextMeshProUGUI mesh)
	{
		_text = mesh;
		_text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]);
		Localization.OnLocaleChanged += (lang) => { _text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]); };
		Localization.FontSwitcher.HookText(_text);
	}
	public void OnLeftButton()
	{
		int prev = (GetPendingIndex() - 1 + _options.Count) % _options.Count;
		_pendingValue = _options[prev];
		if (_pendingValue is Enum)
			_text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]);
	}
	public void OnRightButton()
	{
		int next = (GetPendingIndex() + 1) % _options.Count;
		_pendingValue = _options[next];
		if (_pendingValue is Enum)
			_text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]);
	}
	public void Confirm()
	{
		_configEntry.Value = _pendingValue;
	}
	public void Cancel()
	{
		_pendingValue = _configEntry.Value;
		if (_pendingValue is Enum)
		{
			_text?.text = Localization.GetText(_translationGUID, ModSettingsManager.valueTextRelations[_translationGUID][_pendingValue as Enum]);
		}
	}
}

public class SettingBlock
{
	public struct SettingData
	{
		// public string guid;
		public string titleKey;
		public dynamic cycleConfigEntry;
	}
	public enum TYPE { LOGO, SEPARATOR, SETTING };
	private List<KeyValuePair<TYPE, dynamic>> rows = [];
	public List<KeyValuePair<TYPE, dynamic>> Rows => rows;

	public bool AddSetting(string titleKey, dynamic cycleConfigEntry)
	{
		if (titleKey == null) return false;
		if (cycleConfigEntry == null) return false;

		try
		{
			rows.Add(new(TYPE.SETTING, new SettingData() { titleKey = titleKey, cycleConfigEntry = cycleConfigEntry }));
		}
		catch (System.Exception) { return false; }
		return true;
	}
	public bool AddLogo(Sprite value)
	{
		rows.Add(new(TYPE.LOGO, value));
		return true;
	}
	public bool AddSeparator()
	{
		rows.Add(new(TYPE.SEPARATOR, null));
		return true;
	}
}