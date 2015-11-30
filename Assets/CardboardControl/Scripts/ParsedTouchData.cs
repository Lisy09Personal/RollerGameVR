using UnityEngine;
using System.Collections;

namespace CardboardControll {
	/// <summary>
	/// Dealing with raw touch input from a Cardboard device.
	/// Provide APIs: IsDown(), StillDown(), IsUp()
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
		/// <returns>The game object holding CardboardMain.</returns>
		private GameObject CardboardGameObject() {
			GameObject mainCamera = Camera.main.gameObject;
			return mainCamera.transform.parent.parent.gameObject;
		}

		// TODO: may re-implement this with UniRx
		public void Update() {
			wasTouched |= IsDown();
		}

		/// <summary>
		/// TouchCount can jump for no reason in a Cardboard
		/// but it's too quick to be considered "Moved"
		/// </summary>
		/// <returns>whether Mouse/Touch is down</returns> 
		public bool IsDown() {
			return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved;
		}

		// TODO: may re-implement this with UniRx
		public bool StillDown() {
			return (wasTouched && Input.touchCount > 0);
			//return Input.touchCount > 0; // maybe the line above equals this?
		}

		// TODO: may re-implement this with UniRx by checking the time stream IsDown->!StillDown
		public bool IsUp() {
			if (wasTouched && !StillDown()) {
				wasTouched = false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Prints the debug. Remove later
		/// </summary>
		public void PrintDebug() {
			Debug.Log("--- Touch\ncount: " + Input.touchCount + 
			          "\ntouched: " + wasTouched);
		}
	}
}
