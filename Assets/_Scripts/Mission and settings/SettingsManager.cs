﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour {
    // 

    const string MUSIC_VOLUME_KEY = "master_volume";
    const string DIFFICULTY_KEY = "difficulty";
    const string HIGH_SCORE_KEY = "high_score";
    const string CAMERA_SETTING_KEY = "camera_setting";


    public Slider difficultySlider;
    public Slider volumeSlider;
    private MusicPlayer musicPlayer;
    public LevelManager levelManager;
    public Button fixCamButton;
    public Button followCamButton;
    public ColorBlock selectedColor;
    public ColorBlock deselectedColor;

    // Use this for initialization
    void Start () {
        musicPlayer = FindObjectOfType<MusicPlayer>(); 

        if (SceneManager.GetActiveScene().name == "01b Settings")
        {
            difficultySlider.value = GetDifficulty();
            volumeSlider.value = GetMusicVolume();
        }     

        

        if(GetCameraSetting() == 0)
        {
            followCamButton.colors = deselectedColor;
            fixCamButton.colors = selectedColor;
        }
        else
        {
            fixCamButton.colors = deselectedColor;
            followCamButton.colors = selectedColor;
        }
    }
 
	public static void SetDifficulty(float diff){
        if (diff >= 0 && diff <= 2)
        {
            PlayerPrefs.SetFloat(DIFFICULTY_KEY, diff);
            SettingsStatic.difficulty = (SettingsStatic.Difficulty)diff;
        } else
        {
            Debug.Log("Difficulty setting out of range");
        }
    }

    public static float GetDifficulty()
    {
        return PlayerPrefs.GetFloat(DIFFICULTY_KEY);
    }

    public static void SetMusicVolume(float volume)
    {
        if (volume <= 1 && volume >= 0)
        {
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        }
        else
        {
            Debug.Log("Volume setting out of range");
        }
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY);
    }

    public static void SetHighScore(int score)
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, score);
    }

    public static int GetHighScore() {
        return PlayerPrefs.GetInt(HIGH_SCORE_KEY);
    }

    // Setting 0 means the camera rotates with ship, 1 means camera is fixed up
	public void SetCamera(int cam){
        if (cam == 0 || cam == 1)
        {
            PlayerPrefs.SetInt(CAMERA_SETTING_KEY, cam);

            if (cam == 0)
            {
                followCamButton.colors = selectedColor;
                fixCamButton.colors = deselectedColor;
                SettingsStatic.camSettings = SettingsStatic.CamSettings.player;
            }
            else if (cam == 1)
            {
                fixCamButton.colors = selectedColor;
                followCamButton.colors = deselectedColor;
                SettingsStatic.camSettings = SettingsStatic.CamSettings.north;
            }
        }
        else
        {
            Debug.Log("Cam setting out of range");
        }
	}

    public static int GetCameraSetting()
    {
        return PlayerPrefs.GetInt(CAMERA_SETTING_KEY);
    }

    public void SaveAndExit()
    {
        PlayerPrefs.SetFloat(DIFFICULTY_KEY, difficultySlider.value);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volumeSlider.value);
        
        levelManager.LoadLevel("01a Start");
    }

 }  
