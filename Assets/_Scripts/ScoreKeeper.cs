using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ScoreKeeper : MonoBehaviour {
	public static int score = 0;
	public Text scoreText;

	// Use this for initialization
	void Start () {
		Reset ();
	}
	
	// Update is called once per frame
	public void addScore(){
		score++;
		scoreText.text = "SCORE: " + score;
	}

	public static void Reset(){
		score = 0;
	}
}
