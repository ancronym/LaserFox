using UnityEngine;
using System.Collections;

public class ProjectileController : MonoBehaviour {
	public float projectileDamage = 10f;
    public float lifetime = 10f;    
    public bool fade;

	public ParticleSystem blast;
    SpriteRenderer renderer;    

    // used for alpha channel setting:
    float startTime;    
    float newAlpha;

	// Use this for initialization
	void Start () {
        startTime = Time.timeSinceLevelLoad;       
        renderer = gameObject.GetComponent<SpriteRenderer>();        
	}

    void Update() {
        if (Time.timeSinceLevelLoad > lifetime + startTime) {
            Destroy(gameObject);
        }

        if (fade == true) {
            newAlpha = (1f - (Time.timeSinceLevelLoad - startTime)) / lifetime;
            newAlpha = Mathf.Clamp(newAlpha, 0.5f, 1f);
            renderer.color = new Color(1f, 1f, 1f, newAlpha);  
        }
    }
	
	// Update is called once per frame
	public void Hit(){
		Instantiate (blast, gameObject.transform.position, Quaternion.identity);        
		Destroy (gameObject);
	}

}
