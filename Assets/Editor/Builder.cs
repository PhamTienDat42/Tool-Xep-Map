using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.Assertions;
using LevelPakage;
using System.Linq;

namespace Tool
{
	public class Builder : MonoBehaviour
	{
		[Space(8.0f)]
		[Header("Level Data")]
		[SerializeField] private TextAsset maps;
		[SerializeField] private TextAsset levels;
		[SerializeField] private LevelSO   levelSO;
		[SerializeField] private LevelPakageSO levelPakageSO;

		[Space(8.0f)]
		[Header("TMP_Ref")]
		[SerializeField] private TMP_Text note;
		[SerializeField] private TMP_Text countLevels;
		[SerializeField] private TMP_Text countLvTemplate;
		[SerializeField] private TMP_Text rangeLvTemplate;
		[SerializeField] private TMP_Text stepsToSolveText;
		[SerializeField] private TMP_Text solveNotificationText;

		[SerializeField] private TMP_Dropdown	 saveLevelsDropdown;
		[SerializeField] private TMP_InputField  savedLevelNameInputField;
		[SerializeField] private TMP_InputField  selectStepsToSolveInputField;
		[SerializeField] private TextMeshProUGUI textPrefab;

		[Space(8.0f)]
		[Header("GameObject")]
		[SerializeField] private GameObject gridContainer;
		[SerializeField] private GameObject invalidMapPanel;
		[SerializeField] private GameObject boardMapObject;
		[SerializeField] private GameObject panelBoard;
		[SerializeField] private GameObject unSolvePanel;
		[SerializeField] private GameObject saveSuccessedPanel;
		[SerializeField] private GameObject waitPanel;
		[SerializeField] private ButtonGrid btn;
		[SerializeField] private Transform  boardPosition;

		[Space(8.0f)]
		[Header("List Data")]
		[SerializeField] private List<Sprite> blockImages;
		[SerializeField] private List<char> blockTxt;
		[SerializeField] private List<TMP_Text> blockTMP;
		[SerializeField] private List<GameObject> blockList;
		[SerializeField] private List<GameObject> prefabs;

		private Board board;
		private string str = ""; // string of map is edited/created
		private string blockText;
		private string selectStepsToSolve;

		private int blockIndex	 = 3;
		private int blockCount   = 2;
		private int savedLvIndex = 0;
		private readonly int boardSize = 6;

		private bool selectedBlock = false;
		private bool isEdited = false;

		private List<GameObject> blocks;
		private readonly List<ButtonGrid> buttons = new();
		private readonly List<char> letterInLV = new();
		private readonly List<string> levelFromSourceList = new();
		private readonly Stack<string> previousSteps = new();
		private readonly Dictionary<KeyCode, int> keyMap = new();
		private readonly int[,] prefabIndexes = { { 0, 0 ,0 }, // vertical index => size, horizontal index => stride
								                  { 0, 1, 1 }, // Size 1, Stride 1
												  { 0, 2, 4 }, // Size 2, Stride 1/ Stride 6
												  { 0, 3, 5 }, // Size 3, Stride 1/ Stride 6
												};
		private void Awake()
		{
			Assert.IsNotNull(textPrefab);
			Assert.IsNotNull(maps);
			blocks = new List<GameObject>();
			keyMap.Add(KeyCode.H, Constants.HBlockIndex);
			keyMap.Add(KeyCode.V, Constants.VBlockIndex);
			keyMap.Add(KeyCode.A, Constants.ABlockIndex);
			keyMap.Add(KeyCode.X, Constants.XBlockIndex);
		}

		private void Start()
		{
			note.text = $" Create level {levelSO.Levels.Count + 1}";
			savedLevelNameInputField.text = Constants.ValueTxtStart;
			selectStepsToSolveInputField.text = Constants.ValueTxtStart;
			LoadGrid();
		}

		public void LoadGrid()
		{
			for (int i = 0; i < boardSize * boardSize; ++i)
			{
				var button = Instantiate(btn, gridContainer.transform);
				button.gameObject.SetActive(true);
				button.Index = i;
				buttons.Add(button);
			}
		}

		public void Undo()
		{
			if (previousSteps.Count > 1)
			{
				ClearGrid();
				previousSteps.Pop();
				str = previousSteps.Peek();
				for (int i = 0; i < boardSize * boardSize; ++i)
				{
					buttons[i].BtnText.text = str[i].ToString() == "." ? "" : str[i].ToString();
				}
				LoadGridImage();
			}
		}

		//Change button grid follow horizontal/vertical 2 or 3
		public void ChangeCombineBtn(int index)
		{
			var row = index % boardSize;
			var col = index / boardSize;

			var currentImg = buttons[index].ButtonImage.sprite;
			var currentTxt = buttons[index].BtnText.text;

			if (row < boardSize - 1 && (blockIndex == Constants.ABlockIndex || blockIndex == Constants.HBlockIndex) && blockCount == 2)
			{
				buttons[index + 1].CombineBtn(currentImg, currentTxt);
				return;
			}

			if (row < boardSize - 2 && blockIndex == Constants.HBlockIndex && blockCount == 3)
			{
				buttons[index + 1].CombineBtn(currentImg, currentTxt);
				buttons[index + 2].CombineBtn(currentImg, currentTxt);
				return;
			}

			if (col < boardSize - 1 && blockIndex == Constants.VBlockIndex && blockCount == 2)
			{
				buttons[index + boardSize].CombineBtn(currentImg, currentTxt);
				return;
			}

			if (col < boardSize - 2 && blockIndex == Constants.VBlockIndex && blockCount == 3)
			{
				buttons[index + boardSize].CombineBtn(currentImg, currentTxt);
				buttons[index + 2 * boardSize].CombineBtn(currentImg, currentTxt);
				return;
			}
		}

		private void Update()
		{
			blockText = blockTxt[blockIndex].ToString();
			var isEsc = true;

			foreach (var map in keyMap)
			{
				if (Input.GetKeyDown(map.Key))
				{
					SelectBlock(map.Value, blockList[map.Value]);
					isEsc = false;
					break;
				}
			}

			if (isEsc == true)
			{
				if (Input.GetKeyDown(KeyCode.Escape)) DeselectAll();
			}

			if (selectedBlock)
			{
				if (Input.GetKeyDown(KeyCode.Alpha2))
				{
					SetThirdBlock(false, 2);
				}
				else if (Input.GetKeyDown(KeyCode.Alpha3))
				{
					SetThirdBlock(true, 3);
				}
			}
		}

		private void SelectBlock(int imgIndex, GameObject block)
		{
			DeselectAll();
			selectedBlock = true;
			blockIndex = imgIndex;
			block.SetActive(true);
			UpdateText();
		}

		private void DeselectAll()
		{
			selectedBlock = false;
			blockCount = 2;
			blockList.ForEach(block => block.SetActive(false));
		}

		private void SetThirdBlock(bool active, int blockNum)
		{
			blockList[Constants.HBlockIndex3].SetActive(active);
			blockList[Constants.VBlockIndex3].SetActive(active);
			blockCount = blockNum;
		}

		//when click button blocks UI
		public void OnSelectedBlockBtn(int index)
		{
			SelectBlock(index, blockList[index]);
		}

		public void GetLettersInLevelText()
		{
			for (int i = 0; i < str.Length; ++i)
			{
				if (char.IsLetter(str[i]))
				{
					letterInLV.Add(str[i]);
				}
			}
		}

		public void UpdateText()
		{
			letterInLV.Clear();
			GetMapString();
			previousSteps.Push(str);
			GetLettersInLevelText();
			var tmpTxt = 'B';

			while (letterInLV.Contains(tmpTxt))
			{
				tmpTxt += (char)1;
				tmpTxt = tmpTxt > 'Z' ? tmpTxt = 'B' : tmpTxt;
			}
			blockTMP.ForEach(block => { block.text = tmpTxt.ToString(); });
			blockTxt[Constants.HBlockIndex] = blockTxt[Constants.VBlockIndex] = tmpTxt;
		}

		public void ClearGrid()
		{
			DeselectAll();
			buttons.Clear();
			GameObject[] buttonGrids = GameObject.FindGameObjectsWithTag(Constants.ButtonGrid);
			buttonGrids.ToList().ForEach(buttonGrid => Destroy(buttonGrid));
			LoadGrid();
		}

		public void RefreshStack()
		{
			previousSteps.Clear();
		}

		public void SaveLevelSO()
		{
			GetMapString();
			board = new Board(boardSize, str);
			var moves = board.Solve2(3 * boardSize - 2);

			if (board.IsValidMap == false)
			{
				invalidMapPanel.SetActive(true);
				return;
			}

			if (board.IsAbleToSolve == false)
			{
				solveNotificationText.text = "Can not solve";
				unSolvePanel.SetActive(true);
				return;
			}

			if (isEdited == false)
			{
				levelSO.CreateNewLevelSO(moves.Count - 1, str);
				saveSuccessedPanel.SetActive(true);
			}
			else
			{
				var level = levelSO.Levels[savedLvIndex];
				level.levelStep = moves.Count - 1;
				level.levelValue = str;
				levelSO.Levels[savedLvIndex] = level;
				saveSuccessedPanel.SetActive(true);
			}
		}

		public void DeleteLevelSO()
		{
			ClearGrid();
			levelSO.Levels.Remove(levelSO.Levels[int.Parse(savedLevelNameInputField.text) - 1]);
			LoadSaveLevelSO();
		}

		private void LoadGridImage()
		{
			for (int index = 0; index < boardSize * boardSize; index++)
			{
				var currentTxt = buttons[index].BtnText.text;
				var currentSprite = buttons[index].Button.image.sprite;
				if (string.IsNullOrEmpty(currentTxt) == true)
				{
					continue;
				}

				if (currentTxt == blockTxt[Constants.XBlockIndex].ToString())
				{
					currentSprite = BlockImages[Constants.XBlockIndex];
					continue;
				}

				if (currentTxt == blockTxt[Constants.ABlockIndex].ToString())
				{
					currentSprite = blockImages[Constants.ABlockIndex];
					continue;
				}

				if (index + 1 < buttons.Count)
				{
					ButtonGrid bRight = buttons[index + 1];
					if (bRight.BtnText.text == currentTxt)
					{
						bRight.Button.image.sprite = currentSprite = blockImages[Constants.HBlockIndex];
					}

					if (index + boardSize < buttons.Count)
					{
						ButtonGrid bDown = buttons[index + boardSize];
						if (bDown.BtnText.text == currentTxt)
						{
							bDown.Button.image.sprite = currentSprite = blockImages[Constants.VBlockIndex];
						}
					}
				}
			}
		}

		public void LoadSaveLevelSO()
		{
			ClearGrid();
			isEdited = true;
			var level = int.Parse(savedLevelNameInputField.text) - 1;
			if (string.IsNullOrEmpty(savedLevelNameInputField.text) == false && level < levelSO.Levels.Count)
			{
				savedLvIndex = level;
			}
			else
			{
				savedLevelNameInputField.text = Constants.ValueTxtStart;
				savedLvIndex = 0;
			}
			note.text = $"Edit Level {savedLevelNameInputField.text}";

			var m = levelSO.Levels[savedLvIndex];
			for (int i = 0; i < boardSize * boardSize; ++i)
			{
				buttons[i].BtnText.text = m.levelValue[i].ToString() == "." ? "" : m.levelValue[i].ToString();
			}
			LoadGridImage();
			countLevels.text = $"Level: {savedLvIndex + 1}/{levelSO.Levels.Count}";
		}

		public void ShowSelectedSavedLevelSO(string arg0)
		{
			if (string.IsNullOrEmpty(arg0) != false)
			{
				return;
			}

			ClearGrid();
			note.text = $"Edit level {savedLevelNameInputField.text}";
			savedLvIndex = int.Parse(savedLevelNameInputField.text) - 1;
			var m = levelSO.Levels[savedLvIndex];

			for (int i = 0; i < boardSize * boardSize; ++i)
			{
				buttons[i].BtnText.text = m.levelValue[i].ToString() == "." ? "" : m.levelValue[i].ToString();
			}
			LoadGridImage();
			countLevels.text = $"Level: {savedLvIndex + 1}/{levelSO.Levels.Count}";
		}

		public void LoadLevelFromFile()
		{
			levelFromSourceList.Clear();
			note.text = $" Create level {levelSO.Levels.Count + 1}";
			isEdited = false;
			maps = levelPakageSO.LevelPakage[int.Parse(selectStepsToSolveInputField.text) - 1];
			var lines = maps.text.Split("\n");
			countLvTemplate.text = $"{lines.Length - 1}";
			rangeLvTemplate.text = lines.Length < 100 ? $"1-{lines.Length - 1}" : $"1-100";
			InitializeDropdownOptions();
			selectStepsToSolveInputField.onEndEdit.AddListener(ShowLevelFromSourcefile);
		}

		private void ShowLevelFromSourcefile(string arg0)
		{
			if (arg0 == null)
			{
				selectStepsToSolve = "";
			}
			if (int.Parse(arg0) < 10)
			{
				selectStepsToSolve = "0" + arg0;
			}
			else
			{
				selectStepsToSolve = arg0;
				Debug.Log(arg0);
			}
			InitializeDropdownOptions();
			note.text = $" Create level {levelSO.Levels.Count + 1}";
		}

		private void InitializeDropdownOptions()
		{
			saveLevelsDropdown.ClearOptions();
			List<TMP_Dropdown.OptionData> options = new();

			var lines = maps.text.Split("\n");
			for (int i = 0; i < lines.Length - 1; i++)
			{
				if (i < 100)
				{
					TMP_Dropdown.OptionData option = new(lines[i]);
					options.Add(option);
				}
				levelFromSourceList.Add(lines[i].Split(" ")[1]);
			}
			saveLevelsDropdown.AddOptions(options);
			saveLevelsDropdown.onValueChanged.AddListener(delegate { OnDropdownValueChanged(saveLevelsDropdown); });
		}

		public void OnDropdownValueChanged(TMP_Dropdown dropdown)
		{
			ClearGrid();
			note.text = $" Create level {levelSO.Levels.Count + 1}";
			var index = dropdown.value;
			var lines = maps.text.Split("\n");
			rangeLvTemplate.text = lines.Length < 100 ? $"{index + 1}-{lines.Length - 1}" : $"{index + 1}-100";

			if (savedLvIndex >= 0 && savedLvIndex < levelSO.Levels.Count)
			{
				for (int i = 0; i < boardSize * boardSize; i++)
				{
					buttons[i].BtnText.text = levelFromSourceList[index][i].ToString() == "." ? "" : levelFromSourceList[index][i].ToString();
				}
				LoadGridImage();
			}
		}

		public void RandomLevelTemplate()
		{
			ClearGrid();
			note.text = $" Create level {levelSO.Levels.Count + 1}";
			var lines = maps.text.Split("\n");
			var index = Random.Range(0, lines.Length - 1);
			countLvTemplate.text = $"{index + 1}/{lines.Length - 1}";

			if (savedLvIndex >= 0 && savedLvIndex < levelSO.Levels.Count)
			{
				for (int i = 0; i < boardSize * boardSize; i++)
				{
					buttons[i].BtnText.text = levelFromSourceList[index][i].ToString() == "." ? "" : levelFromSourceList[index][i].ToString();
				}
				LoadGridImage();
			}
		}

		public void CreateNewLevelSO()
		{
			ClearGrid();
			previousSteps.Clear();
			note.text = $" Create level {levelSO.Levels.Count + 1}";
			isEdited = false;
			for (int i = 0; i < boardSize * boardSize; i++)
			{
				buttons[i].BtnText.text = "";
			}
			UpdateText();
		}

		public void GetMapString()
		{
			str = "";
			for (int i = 0; i < boardSize * boardSize; i++)
			{
				str += buttons[i].BtnText.text == "" ? "." : buttons[i].BtnText.text;
			}
			Debug.Log(str.Length + ":" + str);
		}

		public void Call1()
		{
			GetMapString();
			DestroyBlocks();
			board = new Board(boardSize, str);

			if (board.IsValidMap == false)
			{
				invalidMapPanel.SetActive(true);
				return;
			}

			var cells = board.ToString();
			SetBoard(board.Pieces);
			Debug.Log(cells);
			boardMapObject.SetActive(true);
			panelBoard.SetActive(true);
		}

		private void CreateAndSaveObject(int x, int y, GameObject prefab)
		{
			Vector3 objectPosition;
			objectPosition.x = x;
			objectPosition.y = y;
			objectPosition.z = 0.0f;
			var box = Instantiate(prefab);
			box.transform.SetParent(boardPosition);
			box.transform.localPosition = objectPosition;
			box.SetActive(true);
			blocks.Add(box);
		}

		private void DestroyBlocks()
		{
			if (boardMapObject.activeSelf == false)
			{
				blocks.ForEach(block => Destroy(block));
			}
		}

		private void SetBoard(List<Piece> pieces)
		{
			for (int i = 0; i < pieces.Count; i++)
			{
				var index = pieces[i].Position;

				var x = index % boardSize;
;
				var y = index / boardSize;

				var prefabIndex = i == 0 ? 0 : prefabIndexes[pieces[i].Size, (pieces[i].Stride == 1 ? pieces[i].Stride : 2)];

				CreateAndSaveObject(x , -y , prefabs[prefabIndex]);
			}
		}

		private IEnumerator WaitSolvePanel()
		{
			waitPanel.SetActive(true);
			yield return new WaitForSeconds(1.0f);
			waitPanel.SetActive(false);
		}

		public void Solve()
		{
			stepsToSolveText.text = "Steps to solve";
			GetMapString();
			board = new Board(boardSize, str);

			if (board.IsValidMap == false)
			{
				invalidMapPanel.SetActive(true);
				return;
			}

			var moves = board.Solve2(3 * boardSize - 2);
			if (board.IsAbleToSolve == false)
			{
				solveNotificationText.text = "Can not solve";
				unSolvePanel.SetActive(true);
				return;
			}

			StartCoroutine(WaitSolvePanel());
			solveNotificationText.text = "Solved";
			foreach (var move in moves)
			{
				stepsToSolveText.text += $"{(char)('A' + move.PieceIndex)} {(Mathf.Sign(move.Steps) > 0.0f ? '+' : '-')}{Mathf.Abs(move.Steps)}" + "\n";
			}
		}

		//Split file rushour of folgeman
		public void SplitMap2()
		{
			string[] lines = maps.text.Split('\n');
			string outputFolderPath = Path.Combine(Application.dataPath, "Maps");

			Dictionary<string, List<string>> linesByPrefix = new();

			foreach (string line in lines)
			{
				if (line.Length >= 2)
				{
					string prefix = line[..2];
					if (!linesByPrefix.ContainsKey(prefix))
					{
						linesByPrefix[prefix] = new List<string>();
					}
					linesByPrefix[prefix].Add(line);
				}
			}

			foreach (var linePrefix in linesByPrefix)
			{
				string levelName = $"level-{linePrefix.Key}.txt";
				string fullPath = Path.Combine(outputFolderPath, levelName);

				using StreamWriter writer = new(fullPath);
				linePrefix.Value.ForEach(line => writer.WriteLine(line));
			}
			Debug.Log("Processing completed.");
		}

		public string BlockText => blockText;
		public List<Sprite> BlockImages => blockImages;
		public int BlockImgIndex { get => blockIndex; set => blockIndex = value; }
		public bool SelectedBlocked { get => selectedBlock; set => selectedBlock = value; }
	}
}
