using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class Scene01AudioSettingsUITests
{
	private const string MusicPrefKey = "MusicVolume";
	private const string SfxPrefKey = "SFXVolume";

	[TearDown]
	public void TearDown()
	{
		PlayerPrefs.DeleteKey(MusicPrefKey);
		PlayerPrefs.DeleteKey(SfxPrefKey);
	}

	[Test]
	public void BuildInterface_CreatesLeftButtonAndHiddenSettingsPanel()
	{
		GameObject host = new GameObject("Scene01AudioSettingsUITest");

		try
		{
			Scene01AudioSettingsUI ui = host.AddComponent<Scene01AudioSettingsUI>();

			ui.BuildInterface();

			Assert.That(ui.SoundButton, Is.Not.Null);
			Assert.That(ui.SettingsPanel, Is.Not.Null);
			Assert.That(ui.SettingsPanel.activeSelf, Is.False);
			Assert.That(ui.MusicSlider, Is.Not.Null);
			Assert.That(ui.SfxSlider, Is.Not.Null);
			Assert.That(
				ui.SettingsPanel.GetComponent<AudioSettingsController>(),
				Is.Not.Null
			);

			RectTransform buttonTransform = ui.SoundButton.GetComponent<RectTransform>();

			Assert.That(buttonTransform.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
			Assert.That(buttonTransform.anchorMax.x, Is.EqualTo(0f).Within(0.001f));
			Assert.That(buttonTransform.anchorMin.y, Is.EqualTo(0.5f).Within(0.001f));
			Assert.That(buttonTransform.anchorMax.y, Is.EqualTo(0.5f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(host);
		}
	}

	[Test]
	public void TogglePanel_ShowsAndHidesSettingsPanel()
	{
		GameObject host = new GameObject("Scene01AudioSettingsUITest");

		try
		{
			Scene01AudioSettingsUI ui = host.AddComponent<Scene01AudioSettingsUI>();

			ui.BuildInterface();
			ui.TogglePanel();

			Assert.That(ui.SettingsPanel.activeSelf, Is.True);

			ui.TogglePanel();

			Assert.That(ui.SettingsPanel.activeSelf, Is.False);
		}
		finally
		{
			Object.DestroyImmediate(host);
		}
	}

	[Test]
	public void InitializeAudioSettings_LoadsSavedValuesIntoSliders()
	{
		PlayerPrefs.SetFloat(MusicPrefKey, 0.25f);
		PlayerPrefs.SetFloat(SfxPrefKey, 0.6f);

		GameObject controllerObject = new GameObject("AudioSettingsControllerTest");
		GameObject musicObject = new GameObject("MusicSlider");
		GameObject sfxObject = new GameObject("SfxSlider");

		try
		{
			AudioSettingsController controller =
				controllerObject.AddComponent<AudioSettingsController>();
			Slider musicSlider = musicObject.AddComponent<Slider>();
			Slider sfxSlider = sfxObject.AddComponent<Slider>();

			controller.Initialize(null, musicSlider, sfxSlider);

			Assert.That(musicSlider.value, Is.EqualTo(0.25f).Within(0.001f));
			Assert.That(sfxSlider.value, Is.EqualTo(0.6f).Within(0.001f));
		}
		finally
		{
			Object.DestroyImmediate(controllerObject);
			Object.DestroyImmediate(musicObject);
			Object.DestroyImmediate(sfxObject);
		}
	}
}
