
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Data{
	public string Tag;
	public Sprite IconSprite;
	public float IconNormalsize = 4 ,IconFarsize = 2 ;
	public Color32 iconNormalColor = Color.white ,iconFarColor= Color.white;
}


[ExecuteInEditMode] //execute script in edit mode, can be removed.

public class MiniMap : MonoBehaviour {

	[Header("GameObjects")]
	public bool SearchPlayer = false; // search player with "Player" tag.
	public GameObject player;
	public GameObject miniMap_Camera,playerIcon,miniMapUi,OuterTexture,iconsGameobjectsHolder; 

	[Header("Settings")]
	public int iconLayerNumber = 8;
	public float viewScale = 40;
	public float iconRadius = 59;
	public float miniMapScale = 1;
	public bool rotateMapWithPlayer = true;

	public float iconsNormalSize = 4 ,iconsFarSize = 2;
	public Color32 iconsNormalColor = Color.white ,iconsFarColor = Color.white;

	public bool destroyIconOnGameObjectDestroy = true;
	public bool SameSize = true , SameColor = true;

	List<GameObject> icons;
	List<Transform> iconHolder;
	List<int> iconHolderId,iconSize;


	[Header ("Put in the Gameobjects tags, choose icons for them and set the settings.")]
	public Data[] GameObjects;

	void Start () {
		if (SearchPlayer)
			player = GameObject.FindGameObjectWithTag ("Player");
		icons = new List<GameObject>();
		iconHolder = new List<Transform>();
		iconHolderId = new List<int>();
		iconSize = new List<int>();
	}




	void LateUpdate () {

		// set the size of the Ui minimap
		miniMapUi.GetComponent<RectTransform> ().localScale = Vector3.one*miniMapScale;

		if (player) {

			// instantiate the map icons
			for (int x = 0; x < GameObjects.Length; x++) {
				
				GameObject[] FindGameObjects = GameObject.FindGameObjectsWithTag (GameObjects [x].Tag);

				foreach (GameObject obj in FindGameObjects) {

					if (!iconHolderId.Contains (obj.GetInstanceID ())) {
						GameObject newIcon = new GameObject ("icon");
						newIcon.AddComponent<SpriteRenderer>();
						newIcon.transform.eulerAngles = new Vector2 (90,0);
						newIcon.layer = iconLayerNumber; // the "IconLayer"
						newIcon.transform.position = new Vector3 (obj.transform.position.x, 0, obj.transform.position.z);
						newIcon.GetComponent<SpriteRenderer> ().sprite = GameObjects [x].IconSprite;
						newIcon.GetComponent<SpriteRenderer> ().sprite = GameObjects [x].IconSprite;
						newIcon.transform.SetParent (iconsGameobjectsHolder.transform);
						icons.Add (newIcon);
						iconHolder.Add (obj.transform);
						iconHolderId.Add (obj.GetInstanceID ()); 
						iconSize.Add (x);
					}

				}

			} 


			// Update the map icons positions
			for (int i = 0; i < icons.Count; i++) {
				
				if (iconHolder [i] != null) {
					
					Vector3 iconeHolderPos = new Vector3 (iconHolder [i].transform.position.x,transform.position.y-viewScale,iconHolder [i].transform.position.z);
					Vector3 planePosition = new Vector3 (player.transform.position.x, transform.position.y - viewScale, player.transform.position.z);

					if (Vector3.Distance (iconeHolderPos, planePosition) > iconRadius) { 
						Vector3 offset = iconeHolderPos - planePosition;
						icons [i].transform.position = planePosition + Vector3.ClampMagnitude (offset, iconRadius);
						if (SameSize)
							icons [i].transform.localScale = new Vector3 (iconsFarSize, iconsFarSize, iconsFarSize);
						else
							icons [i].transform.localScale = new Vector3 (GameObjects[iconSize[i]].IconFarsize, GameObjects[iconSize[i]].IconFarsize, GameObjects[iconSize[i]].IconFarsize);
						if (SameColor)
							icons [i].GetComponent<SpriteRenderer> ().color = iconsFarColor;
						else
							icons [i].GetComponent<SpriteRenderer> ().color = GameObjects[iconSize[i]].iconFarColor;

					} else {
						icons [i].transform.position = iconeHolderPos;
						if (SameSize)
							icons [i].transform.localScale = new Vector3 (iconsNormalSize, iconsNormalSize, iconsNormalSize);
						else
							icons [i].transform.localScale = new Vector3 (GameObjects[iconSize[i]].IconNormalsize, GameObjects[iconSize[i]].IconNormalsize, GameObjects[iconSize[i]].IconNormalsize);
						if (SameColor)
							icons [i].GetComponent<SpriteRenderer> ().color = iconsNormalColor;
						else
							icons [i].GetComponent<SpriteRenderer> ().color =  GameObjects[iconSize[i]].iconNormalColor;
					}

				} else {
					if (destroyIconOnGameObjectDestroy && icons[i]!=null)
						Destroy (icons[i].gameObject);
				}
			}

			//Update miniMap camera position and rotation
			float cameraHeight = viewScale * 2;
			miniMap_Camera.transform.position = new Vector3 (player.transform.position.x, player.transform.position.y+cameraHeight, player.transform.position.z); // camera position
			if (rotateMapWithPlayer) {
				miniMap_Camera.transform.localRotation = Quaternion.Euler (90, player.transform.eulerAngles.y, 0);
				playerIcon.GetComponent<RectTransform> ().localRotation = Quaternion.Euler (0, 0, 0);
				if (OuterTexture!=null)
					OuterTexture.GetComponent<RectTransform>().rotation = Quaternion.Euler (0,0,-player.transform.eulerAngles.y);
			} else {
				miniMap_Camera.transform.localRotation = Quaternion.Euler (90, 0, 0);
				playerIcon.GetComponent<RectTransform> ().localRotation = Quaternion.Euler (0,0,-player.transform.eulerAngles.y);
				if (OuterTexture!=null)
					OuterTexture.GetComponent<RectTransform>().rotation = Quaternion.Euler (0,0,0);
			}





		}



	}




}
