using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Tool
{
	public class ButtonGrid : MonoBehaviour
	{
		[SerializeField] private Builder  builder;

		[Space(8.0f)]
		[Header("Button Properties")]
		[SerializeField] private Button	  btn;
		[SerializeField] private Image	  btnImage;
		[SerializeField] private TMP_Text btnText;
		private int index;

		private void Start()
		{
			btn.onClick.AddListener(ClickButton);
		}

		private void ClickButton()
		{
			var defaultSprite = builder.BlockImages[Constants.DefaultIndex];
			btnImage.sprite   = (btnImage.sprite == defaultSprite) ? builder.BlockImages[builder.BlockImgIndex] : defaultSprite;
			btnText.text	  = string.IsNullOrEmpty(btnText.text) == true ? builder.BlockText : "";
			builder.ChangeCombineBtn(index);
			builder.UpdateText();
		}

		public void CombineBtn(Sprite currentImg, string currentTxt)
		{
			btnText.text	= currentTxt;
			btnImage.sprite = currentImg;
		}

		public int Index { get => index; set => index = value; }
		public TMP_Text BtnText => btnText;
		public Button Button => btn;
		public Image ButtonImage { get => btnImage; set => btnImage = value; }
	}
}
