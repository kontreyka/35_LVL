using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
	[Header("Audio Mixer")]
	[SerializeField] private AudioMixer audioMixer;

	[Header("Sliders")]
	[SerializeField] private Slider musicSlider;
	[SerializeField] private Slider sfxSlider;

	private const string MusicVolumeParameter = "MusicVolume";
	private const string SFXVolumeParameter = "SFXVolume";
	private const string MusicPrefKey = "MusicVolume";
	private const string SfxPrefKey = "SFXVolume";

	private bool isInitialized;

	private void Start()
	{
		InitializeSliders();
	}

	private void OnDestroy()
	{
		if (musicSlider != null)
		{
			musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
		}

		if (sfxSlider != null)
		{
			sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
		}
	}

	public void Initialize(AudioMixer mixer, Slider music, Slider sfx)
	{
		audioMixer = mixer;
		musicSlider = music;
		sfxSlider = sfx;

		InitializeSliders();
	}

	private void InitializeSliders()
	{
		if (isInitialized)
			return;

		if (musicSlider == null || sfxSlider == null)
		{
			Debug.LogWarning("AudioSettingsController: не назначены слайдеры звука.");
			return;
		}

		float musicValue = PlayerPrefs.GetFloat(MusicPrefKey, 0.8f);
		float sfxValue = PlayerPrefs.GetFloat(SfxPrefKey, 0.8f);

		musicSlider.SetValueWithoutNotify(musicValue);
		sfxSlider.SetValueWithoutNotify(sfxValue);

		SetMusicVolume(musicValue);
		SetSFXVolume(sfxValue);

		musicSlider.onValueChanged.AddListener(SetMusicVolume);
		sfxSlider.onValueChanged.AddListener(SetSFXVolume);

		isInitialized = true;
	}

	public void SetMusicVolume(float value)
	{
		float clampedValue = Mathf.Clamp01(value);

		if (audioMixer != null)
		{
			audioMixer.SetFloat(
				MusicVolumeParameter,
				ConvertToDecibels(clampedValue)
			);
		}

		PlayerPrefs.SetFloat(MusicPrefKey, clampedValue);
	}

	public void SetSFXVolume(float value)
	{
		float clampedValue = Mathf.Clamp01(value);

		if (audioMixer != null)
		{
			audioMixer.SetFloat(
				SFXVolumeParameter,
				ConvertToDecibels(clampedValue)
			);
		}

		PlayerPrefs.SetFloat(SfxPrefKey, clampedValue);
	}

	private float ConvertToDecibels(float value)
	{
		if (value <= 0.0001f)
			return -80f;

		return Mathf.Log10(value) * 20f;
	}
}
