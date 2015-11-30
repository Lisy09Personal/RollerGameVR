using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CardboardControll {
	/// <summary>
	/// Dealing with raw magnet input from a Cardboard device.
	/// Using Input.compass API of Unity.
	/// Now only support two position state for the magnet swtich.
	/// May suppurt sth likt GearVR Touchpad later?
	/// </summary>
	public class ParsedMagnetData {
		/*
		public void Dummy()
		{
			IObservable<bool> source;
			var switchObservable = source.DistinctUntilChanged ();
			var downObservable = switchObservable.Where (isDown => isDown);
			var upObservable = switchObservable.Where (isDown => !isDown);

			var duringDownObservable = source.SkipUntil(downObservable)
				.TakeUntil(upObservable)
				.Repeat();

			var longPress2Seconds = downObservable.Delay(TimeSpan.FromSeconds(2f))
				.TakeUntil(upObservable)
				.Repeat();

			var pressTime = source
				.Timestamp()
				.CombineLatest(
					switchObservable.Timestamp(), 
					(left, right) => new Timestamped<bool>(left.Value, left.Timestamp - right.Timestamp)
				);


			switchObservable
				.Timestamp()
				.Scan((prev, current) => new Timestamped<bool>(current.Value, current.Timestamp - prev.Timestamp))
		}
		*/


		/// struct that handles deltaTime and y of the magnet switch
		private struct MagnetMoment {
			public float deltaTime;
			public float yMagnitude;
			public MagnetMoment(float deltaTime, float yMagnitude) {
				this.deltaTime = deltaTime;
				this.yMagnitude = yMagnitude;
			}
		}

		/// The two state now of the magnet swtich.
		private struct MagnetWindowState {
			/// Approximates how still the device is over time
			public float ratio; 
			/// Approximates how fast the device is moving relative to the magnet
			public float delta;
		}

		private List<MagnetMoment> magnetWindow;
		private MagnetWindowState currentMagnetWindow;
		// TODO: these parameters may need adjustment
		private float MAX_WINDOW_SECONDS = 0.1f;
		private float MAGNET_RATIO_MIN_THRESHOLD = 0.03f;
		private float MAGNET_RATIO_MAX_THRESHOLD = 0.2f;
		private float MAGNET_MAGNITUDE_THRESHOLD = 200.0f;
		private float STABLE_RATIO_THRESHOLD = 0.001f;
		private float STABLE_DELTA_THRESHOLD = 2.0f;
		private float windowLength = 0.0f;
		
		enum TriggerState {
			Negative,
			Neutral,
			Positive
		};
		private TriggerState triggerState = TriggerState.Neutral;
		private bool isDown = false;
		private bool isStable = false;
		
		// TODO: things get messed up when you insert the device into the magnet cardboard for the first time!
		public ParsedMagnetData() {
			Input.compass.enabled = true;
			magnetWindow = new List<MagnetMoment>();
			windowLength = 0.0f;
		}
		
		public void Update() {
			TrimMagnetWindow();
			AddToMagnetWindow();
			currentMagnetWindow = CaptureMagnetWindow();
			
			TriggerState newTriggerState = CheckTriggerState();
			isStable = CheckStability();
			if (!isStable) {
				ResetState();
			} 

			if (isStable && newTriggerState != TriggerState.Neutral && triggerState != newTriggerState) {
				isDown = !isDown;
				triggerState = newTriggerState;
			}
		}

		/// <summary>
		/// Trims the magnet window from the head until its length <= MAX_WINDOW_SECONDS.
		/// Note that if the last window's deltatime > MAX_WINDOW_SECONDS, the window may be cleared.
		/// </summary>
		public void TrimMagnetWindow() {
			while (windowLength > MAX_WINDOW_SECONDS) {
				MagnetMoment moment = magnetWindow[0];
				magnetWindow.RemoveAt(0);
				windowLength -= moment.deltaTime;
			}
		}

		/// <summary>
		/// Adds to magnet window.
		/// </summary>
		public void AddToMagnetWindow() {
			magnetWindow.Add(new MagnetMoment(Time.deltaTime, Input.compass.rawVector.magnitude));
			windowLength += Time.deltaTime;
		}

		// -- private methods --

		/// <summary>
		/// Captures current magnet state by checking the magnet window.
		/// Note that the tail of the window is always updated by AddToMagnetWindow().
		/// The ratio of Current State = AverageYMagnitude(firstHalf) / AverageYMagnitude(lastHalf).
		/// </summary>
		/// <returns>The current magnet window.</returns>
		private MagnetWindowState CaptureMagnetWindow() {
			MagnetWindowState newState = new MagnetWindowState();
			int midIndex = magnetWindow.Count / 2;
			List<MagnetMoment> firstHalf = magnetWindow.GetRange(0, midIndex);
			// lastHalf has at least one member and its yMagnitude should be > 0!
			List<MagnetMoment> lastHalf = magnetWindow.GetRange(midIndex, magnetWindow.Count - midIndex);
			newState.ratio = AverageYMagnitude(firstHalf) / AverageYMagnitude(lastHalf);
			newState.delta = Mathf.Abs(magnetWindow[magnetWindow.Count-1].yMagnitude 
			                           - magnetWindow[0].yMagnitude);
			
			return newState;
		}

		private float AverageYMagnitude(List<MagnetMoment> moments) {
			if (moments.Count == 0) 
				return 0.0f;
			float sum = 0.0f;
			for (int index = 0; index < moments.Count; index++) {
				sum += moments[index].yMagnitude;
			}
			return sum / moments.Count;
		}

		/// <summary>
		/// Checks the state of the trigger.
		/// </summary>
		/// <returns>The trigger state.</returns>
		private TriggerState CheckTriggerState() {
			if (IsNegative ()) {
				return TriggerState.Negative;
			} else if (IsPositive ()) {
				return TriggerState.Positive;
			} else {
				return TriggerState.Neutral;
			}
		}

		/// <summary>
		/// Checks the stability.
		/// </summary>
		/// <returns>isStable</returns>
		private bool CheckStability() {
			if (MagnetAbsent ()) {
				return false;
			} else if (currentMagnetWindow.delta < STABLE_DELTA_THRESHOLD &&
			           currentMagnetWindow.ratio < 1f + STABLE_RATIO_THRESHOLD &&
			           currentMagnetWindow.ratio > 1f - STABLE_RATIO_THRESHOLD) {
				return true;
			} else {
				return isStable;
			}
		}

		/// <summary>
		/// Whether other magnets are absent.
		/// </summary>
		/// <returns>In the absence of a stronger magnet, it will measure the Earth's filed.</returns>
		private bool MagnetAbsent() {
			return Input.compass.rawVector.magnitude < MAGNET_MAGNITUDE_THRESHOLD;
		}

		/// <summary>
		/// true if yMagnitude of the firstHalf < that of the lastHalf & the value in certain region
		/// </summary>
		/// <returns></returns>
		private bool IsNegative() {
			return (currentMagnetWindow.ratio < 1f-MAGNET_RATIO_MIN_THRESHOLD &&
			        currentMagnetWindow.ratio > 1f-MAGNET_RATIO_MAX_THRESHOLD);
		}

		/// <summary>
		/// true if yMagnitude of the firstHalf ? that of the lastHalf & the value in certain region
		/// </summary>
		/// <returns></returns>
		private bool IsPositive() {
			return (currentMagnetWindow.ratio > 1f+MAGNET_RATIO_MIN_THRESHOLD &&
			        currentMagnetWindow.ratio < 1f+MAGNET_RATIO_MAX_THRESHOLD);
		}
		
		private bool IsMagnetGoingDown(float min, float max, float start) {
			float minDelta = Mathf.Abs(min - start);
			float maxDelta = Mathf.Abs(max - start);
			return minDelta > maxDelta;
		}
		
		public bool IsDown() {
			return triggerState != TriggerState.Neutral && isDown;
		}
		
		public bool IsUp() {
			return triggerState != TriggerState.Neutral && !isDown;
		}

		public void ResetState() {
			triggerState = TriggerState.Neutral;
			isDown = false;
		}

		public void PrintDebug() {
			Debug.Log("--- Magnetometer\nmagnitude: " + Input.compass.rawVector.magnitude +
			          "\nratio: " + currentMagnetWindow.ratio +
			          "\ndelta: " + currentMagnetWindow.delta + 
			          "\nstable: " + isStable +
			          "\nstate: " + triggerState);
		}
	}
}