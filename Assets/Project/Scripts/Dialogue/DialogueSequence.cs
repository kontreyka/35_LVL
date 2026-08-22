using System;
using UnityEngine;

[CreateAssetMenu(
	fileName = "DialogueSequence",
	menuName = "Project/Dialogue/Dialogue Sequence"
)]
public sealed class DialogueSequence : ScriptableObject
{
	[SerializeField] private DialogueLine[] lines = Array.Empty<DialogueLine>();

	public int LineCount => lines == null ? 0 : lines.Length;

	public bool HasLines => LineCount > 0;

	public DialogueLine GetLine(int index)
	{
		return lines[index];
	}
}

[Serializable]
public sealed class DialogueLine
{
	[SerializeField] private string speakerName;
	[SerializeField, TextArea(2, 6)] private string text;
	[SerializeField] private Sprite portrait;
	[SerializeField] private bool centerText;
	[SerializeField, Min(0f)] private float fontSizeOverride;

	public string SpeakerName => speakerName;
	public string Text => text;
	public Sprite Portrait => portrait;
	public bool CenterText => centerText;
	public float FontSizeOverride => fontSizeOverride;
}
