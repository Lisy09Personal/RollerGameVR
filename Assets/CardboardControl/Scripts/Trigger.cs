using UnityEngine;
using System.Collections;
using CardboardControlDelegates;
using System;

namespace CardboardControll {
	/// <summary>
	/// Provide APIs for the magnet trigger by the ParsedMagnetData&ParsedTouchData classes (check the compass directly).
	/// </summary>
	public class Trigger : MonoBehaviour {
		public float clickSpeedThreshold = 0.4f;
		public bool vibrateOnDown = false;
		public bool vibrateOnUp = false;
		public bool vibrateOnClick = true;
		public KeyCode triggerKey = KeyCode.Space;
		public bool printDebugInfo = false;
		
		private ParsedMagnetData magnet;
		private ParsedTouchData touch;
		private enum TriggerState { Up, Down }
		private TriggerState currentTriggerState = TriggerState.Up;
		private float clickStartTime = 0f;

		private int debugThrottle = 0;
		private int FRAMES_PER_DEBUG = 5;

		public event Action OnUp;
		public event Action OnDown;
		public event Action OnClick;

		private static Trigger instance = null;
		public static Trigger Instance
		{
			get
			{
				if (instance == null) {
					instance = UnityEngine.Object.FindObjectOfType<Trigger>();
				}
				return instance;
			}
		}

		void Awake() {
			if (instance == null) {
				instance = this;
			}
			if (instance != this) {
				Debug.LogWarning("CardboardControll object should be a singleton.");
				enabled = false;
				return;
			}
			#if UNITY_IOS
			Application.targetFrameRate = 60;
			#endif
			// Prevent the screen from dimming / sleeping
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}
		
		public void Start() {
			magnet = new ParsedMagnetData();
			touch = new ParsedTouchData();
		}
		
		public void Update() {
			magnet.Update();
			touch.Update();
			CheckTouch();
			CheckMagnet();
			CheckKey();
		}

		public void FixedUpdate() {
			if (printDebugInfo) {
				PrintDebug();
			}
		}
		
		private bool KeyFor(string direction) {
			switch(direction) {
			case "down":
				return Input.GetKeyDown(triggerKey);
			case "up":
				return Input.GetKeyUp(triggerKey);
			default:
				return false;
			}
		}
		
		private void CheckKey() {
			if (KeyFor ("down")) {
				ReportDown ();
			} else if (KeyFor ("up")) {
				ReportUp ();
			}
		}
		
		private void CheckMagnet() {
			if (magnet.IsDown ()) {
				ReportDown ();
			} else if (magnet.IsUp()) {
				ReportUp();
			}
		}
		
		private void CheckTouch() {
			if (touch.IsDown()) ReportDown();
			if (touch.IsUp()) ReportUp();
		}
		
		private bool IsTouching() {
			return Input.touchCount > 0;
		}
		
		private void ReportDown() {
			if (currentTriggerState == TriggerState.Up) {
				currentTriggerState = TriggerState.Down;
				if (OnDown != null) {
					OnDown();
				}
				if (vibrateOnDown) {
					Handheld.Vibrate();
				}
				clickStartTime = Time.time;
			}
		}
		
		private void ReportUp() {
			if (currentTriggerState == TriggerState.Down) {
				currentTriggerState = TriggerState.Up;
				if (OnUp!= null) {
					OnUp();
				}
				if (vibrateOnUp) {
					Handheld.Vibrate();
				}
				CheckForClick();
			}
		}
		
		private void CheckForClick() {
			bool withinClickThreshold = SecondsHeld() <= clickSpeedThreshold;
			clickStartTime = 0f;
			if (withinClickThreshold) {
				ReportClick ();
			}
		}
		
		private void ReportClick() {
			if (OnClick != null) {
				OnClick ();
			}
			if (vibrateOnClick) {
				Handheld.Vibrate ();
			}
		}
		
		public float SecondsHeld() {
			return Time.time - clickStartTime;
		}
		
		public bool IsHeld() {
			return (currentTriggerState == TriggerState.Down);
		}
		
		public string SensorChart() {
			return MagnetChart() + "\n\n" + TouchChart();
		}
		
		public string MagnetChart() {
			string chart = "Magnet Readings\n";
			chart += magnet.IsDown() ? "vvv " : "    ";
			chart += magnet.IsUp() ? "^^^ " : "    ";
			return chart;
		}
		
		public string TouchChart() {
			string chart = "Touch Reading: ";
			chart += IsTouching() ? "touching" : "--";
			return chart;
		}
		
		public string StateChart() {
			string chart = "";
			chart += "Trigger State: ";
			chart += IsHeld() ? "down" : "up";
			return chart;
		}

		private void PrintDebug() {
			debugThrottle++;
			if (debugThrottle >= FRAMES_PER_DEBUG) {
				magnet.PrintDebug();
				touch.PrintDebug();
				debugThrottle = 0;
			}
		}
	}

}