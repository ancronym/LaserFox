using UnityEngine;
using System.Collections;

public class Link : MonoBehaviour
{

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	public void LinkToAsset ()
	{
		Application.OpenURL ("https://www.assetstore.unity3d.com/#!/content/82873");
	}

}

