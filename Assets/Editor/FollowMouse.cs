using UnityEngine;

namespace Tool
{
	public class FollowMouse : MonoBehaviour
	{
		[SerializeField] private Camera mainCamera;
		[SerializeField] private Builder builder;
		private Vector3 mousePosition;

		private void Update()
		{
			if (builder.SelectedBlocked == true)
			{
				mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
				transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
			}
		}
	}
}
