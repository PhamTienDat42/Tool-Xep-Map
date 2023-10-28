using System.Collections.Generic;
using UnityEngine;

namespace LevelPakage
{
	[CreateAssetMenu(fileName = "Level", menuName = "Maps/Level")]
	public class LevelSO : ScriptableObject
	{
		[System.Serializable]
		public struct LevelData
		{
			public int levelStep;
			public string levelValue;
		}

		[SerializeField] private List<LevelData> levels = new List<LevelData>();

		public void CreateNewLevelSO(int step, string value)
		{
			LevelData newLevel = new LevelData
			{
				levelStep = step,
				levelValue = value
			};

			levels.Add(newLevel);
		}

		public List<LevelData> Levels { get => levels; set => levels = value; }
	}
}

