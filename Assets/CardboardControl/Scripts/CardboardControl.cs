﻿using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using CardboardControlDelegates;

namespace CardboardControll {
/**
* Bring all the control scripts together to provide a convenient API
*/
public class CardboardControl : MonoBehaviour {
  [HideInInspector]
  public Trigger trigger;
  [HideInInspector]
  public CardboardControlGaze gaze;
  [HideInInspector]
  public CardboardControlBox box;

  public bool debugChartsEnabled = false;

  public void Awake() {
    trigger = Trigger.Instance;
    gaze = gameObject.GetComponent<CardboardControlGaze>();
    box = gameObject.GetComponent<CardboardControlBox>();
			
	// Prevent the screen from dimming / sleeping
	Screen.sleepTimeout = SleepTimeout.NeverSleep;
  }

  public void Update() {
    if (debugChartsEnabled) {
		PrintDebugCharts();
	}
  }

  public void PrintDebugCharts() {
    Debug.Log(trigger.SensorChart());
    Debug.Log(trigger.StateChart());
  }
}

}