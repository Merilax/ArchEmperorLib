using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArchEmperorLib;

public static class ModCreditsManager
{
	public static GameObject GenerateBlock(string guid, CreditBlock creditBlock)
	{
		try
		{
			GameObject tempCreditsObject = new(guid + "Credits");
			VerticalLayoutGroup blockGroup = tempCreditsObject.AddComponent<VerticalLayoutGroup>();
			blockGroup.padding = new(20, 20, 20, 20);
			blockGroup.spacing = 20;
			blockGroup.childAlignment = TextAnchor.MiddleCenter;
			blockGroup.childForceExpandHeight = false;
			blockGroup.childForceExpandWidth = false;
			blockGroup.childControlHeight = true;
			blockGroup.childControlWidth = true;

			foreach (var item in creditBlock.Rows)
			{
				LayoutElement layoutElement;
				switch (item.Key)
				{
					case CreditBlock.TYPE.SEPARATOR:
						GameObject separator = new("Separator");
						separator.transform.SetParent(tempCreditsObject.transform);
						Image sepImg = separator.AddComponent<Image>();
						sepImg.color = new(.7f, .7f, .7f);
						layoutElement = separator.AddComponent<LayoutElement>();
						layoutElement.minHeight = 6;
						layoutElement.flexibleWidth = 1;
						break;
					case CreditBlock.TYPE.LOGO:
						GameObject logo = new("Logo");
						logo.transform.SetParent(tempCreditsObject.transform);
						layoutElement = logo.AddComponent<LayoutElement>();
						layoutElement.minHeight = 170;
						layoutElement.minWidth = 170;
						layoutElement.preferredHeight = 200;
						layoutElement.preferredWidth = 200;
						Sprite logoSprite = item.Value;
						Image logoImg = logo.AddComponent<Image>();
						logoImg.sprite = logoSprite;
						break;
					case CreditBlock.TYPE.TITLE:
					case CreditBlock.TYPE.ROLE:
					case CreditBlock.TYPE.TEXT:
					default:
						GameObject row = new("Text");
						row.transform.SetParent(tempCreditsObject.transform);
						TextMeshProUGUI text = row.AddComponent<TextMeshProUGUI>();
						text.text = item.Value;
						text.font = UIUtils.Fonts.mainFont;
						text.fontMaterial = UIUtils.Fonts.mainFontMat;
						text.color = new(.1f, .1f, .1f);
						text.textWrappingMode = TextWrappingModes.Normal;
						text.alignment = TextAlignmentOptions.Center;
						layoutElement = row.AddComponent<LayoutElement>();
						layoutElement.flexibleWidth = 1;
						switch (item.Key)
						{
							case CreditBlock.TYPE.TITLE:
								text.fontSize = 46; layoutElement.minHeight = 50; break;
							case CreditBlock.TYPE.ROLE:
								text.fontSize = 30; layoutElement.minHeight = 34; break;
							case CreditBlock.TYPE.TEXT:
							default:
								text.fontSize = 24; layoutElement.minHeight = 28; break;
						}
						break;
				}
			}
			return tempCreditsObject;
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogError(ex);
			return null;
		}
	}

	public static bool AddBundlesIfAny(string guid, GameObject creditsObject)
	{
		try
		{
			var bundleProvider = ModRegistry.GetBundles(guid);
			if (bundleProvider != null)
			{
				GameObject bundleContainer = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("CreditBundleList"));
				bundleContainer.transform.SetParent(creditsObject.transform);

				var sampleRow = bundleContainer.transform.Find("VBox").Find("Row").gameObject;
				Transform rowContainer = bundleContainer.transform.Find("VBox");

				foreach (var bundle in bundleProvider())
				{
					var row = GameObject.Instantiate(sampleRow);
					row.transform.SetParent(rowContainer);
					row.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = bundle.Name;
					row.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = bundle.Version;
					row.active = true;
				}
				return true;
			}
			return false;
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogError(ex);
			return false;
		}
	}
}

public class CreditBlock
{
	public enum TYPE { LOGO, TITLE, ROLE, TEXT, SEPARATOR };
	public enum FONT_SIZE { TITLE, ROLE, NORMAL };
	private List<KeyValuePair<TYPE, dynamic>> rows = [];
	public List<KeyValuePair<TYPE, dynamic>> Rows => rows;

	public bool AddText(FONT_SIZE key, string value)
	{
		var textType = key switch
		{
			FONT_SIZE.TITLE => TYPE.TITLE,
			FONT_SIZE.ROLE => TYPE.ROLE,
			_ => TYPE.TEXT,
		};
		rows.Add(new(textType, value));
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