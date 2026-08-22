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

	private void Start()
	{
		musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
		sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

		SetMusicVolume(musicSlider.value);
		SetSFXVolume(sfxSlider.value);

		musicSlider.onValueChanged.AddListener(SetMusicVolume);
		sfxSlider.onValueChanged.AddListener(SetSFXVolume);
	}

	public void SetMusicVolume(float value)
	{
		audioMixer.SetFloat(
			MusicVolumeParameter,
			ConvertToDecibels(value)
		);

		PlayerPrefs.SetFloat("MusicVolume", value);
	}

	public void SetSFXVolume(float value)
	{
		audioMixer.SetFloat(
			SFXVolumeParameter,
			ConvertToDecibels(value)
		);

		PlayerPrefs.SetFloat("SFXVolume", value);
	}

	private float ConvertToDecibels(float value)
	{
		if (value <= 0.0001f)
			return -80f;

		return Mathf.Log10(value) * 20f;
	}
}