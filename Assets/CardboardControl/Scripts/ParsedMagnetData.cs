﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CardboardControll {
	/// <summary>
	/// Dealing with raw magnet input from a Cardboard device.
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

		/// the two state now of the magnet swtich
		private struct MagnetWindowState {
			public float firstHalf;
			public float lastHalf;
			// TODO: only need this?
			public float ratio; 
		}

		private List<MagnetMoment> magnetWindow;
		private MagnetWindowState currentMagnetWindow;
		private const float MAX_WINDOW_SECONDS = 0.2f;
		private const float MAGNET_RATIO_MIN_THRESHOLD = 0.05f;
		private float MAGNET_RATIO_MAX_THRESHOLD = 0.1f;
		private float CALIBRATION_SECONDS = 1f;
		private float MAGNITUDE_THRESHOLD = 150.0f;
		private float windowLength = 0.0f;
		
		enum TriggerState {
			Negative,
			Neutral,
			Postive
		};
		private bool wasTriggering = false;
		private TriggerState triggerState = TriggerState.Neutral;
		private bool isDown = false;
		
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
			
			TriggerState newTriggerState = GetTriggerState();
			// Debug.Log(Input.compass.rawVector.magnitude + "\n" + newTriggerState + "\n" + triggerState);
			
			if (newTriggerState != TriggerState.Neutral && triggerState != newTriggerState) {
				isDown = !isDown;
				triggerState = newTriggerState;
			}
		}
		private TriggerState GetTriggerState() {
			if (Time.time < CALIBRATION_SECONDS) return TriggerState.Neutral;
			if (Input.compass.rawVector.magnitude < MAGNITUDE_THRESHOLD) {
				triggerState = TriggerState.Neutral;
				return TriggerState.Neutral;
			}
			if (IsNegative()) return TriggerState.Negative;
			if (IsPositive()) return TriggerState.Postive;
			return TriggerState.Neutral;
		}
		
		private bool IsNegative() {
			return (currentMagnetWindow.ratio < 1f-MAGNET_RATIO_MIN_THRESHOLD &&
			        currentMagnetWindow.ratio > 1f-MAGNET_RATIO_MAX_THRESHOLD);
		}
		
		private bool IsPositive() {
			return (currentMagnetWindow.ratio > 1f+MAGNET_RATIO_MIN_THRESHOLD &&
			        currentMagnetWindow.ratio < 1f+MAGNET_RATIO_MAX_THRESHOLD);
		}
		
		public void TrimMagnetWindow() {
			while (windowLength > MAX_WINDOW_SECONDS) {
				MagnetMoment moment = magnetWindow[0];
				magnetWindow.RemoveAt(0);
				windowLength -= moment.deltaTime;
			}
		}
		
		public void AddToMagnetWindow() {
			magnetWindow.Add(new MagnetMoment(Time.deltaTime, Input.compass.rawVector.magnitude));
			windowLength += Time.deltaTime;
		}
		
		private MagnetWindowState CaptureMagnetWindow() {
			MagnetWindowState newState = new MagnetWindowState();
			int middle = magnetWindow.Count / 2;
			List<MagnetMoment> firstHalf = magnetWindow.GetRange(0, middle);
			List<MagnetMoment> lastHalf = magnetWindow.GetRange(middle, magnetWindow.Count - middle);
			newState.firstHalf = Average(firstHalf);
			newState.lastHalf = Average(lastHalf);
			newState.ratio = newState.firstHalf / newState.lastHalf;
			return newState;
		}
		
		private float Average(List<MagnetMoment> moments) {
			if (moments.Count == 0) return 0.0f;
			float sum = 0.0f;
			for (int index = 0; index < moments.Count; index++) {
				sum += moments[index].yMagnitude;
			}
			return sum / moments.Count;
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
	}
}