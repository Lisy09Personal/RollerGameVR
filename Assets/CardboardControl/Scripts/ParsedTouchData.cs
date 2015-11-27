﻿using UnityEngine;
using System.Collections;

namespace CardboardControll {
	/// <summary>
	/// Dealing with raw touch input from a Cardboard device
	/// </summary>
	public class ParsedTouchData {
		// 
		private bool wasTouched = false;
		
		public ParsedTouchData() {
			// TODO: may need to change the type "Cardboard" when Google API changes
			Cardboard cardboard = CardboardGameObject().GetComponent<Cardboard>();
			// init the magnet trigger of cardboard 
			cardboard.TapIsTrigger = false;
		}
		
		/// <summary>
		/// Get the game object of Cardboard. Note that the camera is placed as Player.CardboardMain.Head.MainCamera
		/// </summary>
		/// <returns>The game object.</returns>
		private GameObject CardboardGameObject() {
			GameObject mainCamera = Camera.main.gameObject;
			return mainCamera.transform.parent.parent.gameObject;
		}

		// TODO: may re-implement this with UniRx
		public void Update() {
			wasTouched |= IsDown();
		}

		/// <summary>
		/// ouchCount can jump for no reason in a Cardboard
		/// but it's too quick to be considered "Moved"
		/// </summary>
		/// <returns>whether Mouse/Touch is down</returns> 
		public bool IsDown() {
			return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved;
		}

		// TODO: may re-implement this with UniRx
		public bool StillDown() {
			return Input.touchCount > 0;
		}
		
		public bool IsUp() {
			if (!StillDown() && wasTouched) {
				wasTouched = false;
				return true;
			}
			return false;
		}
	}

}