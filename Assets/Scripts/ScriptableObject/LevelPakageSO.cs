using System.Collections.Generic;
using UnityEngine;

namespace LevelPakage
{
	[CreateAssetMenu(fileName = "Levelpakage", menuName = "Maps/Levelpakage")]
	public class LevelPakageSO : ScriptableObject
	{
		[SerializeField] private List<TextAsset> levelPakage;
		public List<TextAsset> LevelPakage => levelPakage;
	}
}

