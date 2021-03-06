﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
	private Rigidbody rb;
	public float speed;
	private int count;
	public Text countText;
	public Text winTextLeft;
	public Text winTextRight;
	public GameObject camera;

	// may put this in other places
	private int winCount = 7;

	void Start(){
		rb = GetComponent<Rigidbody> ();
		winTextRight.text = "";
		winTextLeft.text = "";
		setCount (0);
	}

	void FixedUpdate(){
		/*float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		Vector3 movement = new Vector3 (moveHorizontal,0,moveVertical);*/
		Vector3 movement = camera.transform.forward;
		rb.AddForce (movement * speed );
	}

	void OnTriggerEnter(Collider other) {
		Debug.Log ("collision:"+other.gameObject.tag );
		if (other.gameObject.CompareTag ("pickup")) {
			other.gameObject.SetActive(false) ;
			count++;
			setCount(count);
		}
	}

	void setCount(int val) {
		count = val;
		countText.text = "Count: " + count.ToString();
		if (val >= winCount) {
			winTextLeft.text = "You Win!";
			winTextRight.text = "You Win!";
		}
	}
}
