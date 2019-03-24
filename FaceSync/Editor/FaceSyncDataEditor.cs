﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FaceSync
{
	[CustomEditor(typeof(FaceSyncData))]
	public class FaceSyncDataEditor : Editor
	{
		private int mSelectedKeyframe;

		public FaceSyncDataEditor()
		{
			mSelectedKeyframe = -1;
		}

		// --------------------------------------------------------------------

		public override void OnInspectorGUI()
		{
			//base.OnInspectorGUI();

			serializedObject.Update();

			FaceSyncData syncData = target as FaceSyncData;
			float dataDuration = syncData.GetDuration();

            EditorGUI.BeginChangeCheck();
            
            syncData.Sound = EditorGUILayout.ObjectField(syncData.Sound, typeof(AudioClip), false, null) as AudioClip;

			if (GUILayout.Button("Play"))
				PlayClip(syncData.Sound);

			EditorGUILayout.Separator();

            syncData.ReferenceText = EditorGUILayout.TextArea(syncData.ReferenceText);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(target);

            if (GUILayout.Button("Autodetect"))
				AutodetectWithRules();

			if (GUILayout.Button("Clear"))
				syncData.Keyframes.Clear();

			float border = 10;
			float initY = 200;
			float width = EditorGUIUtility.currentViewWidth;
			Rect barRect = new Rect(border, initY, width - (border * 2), 5);
			if (GUI.Button(barRect,""))
			{
				// TODO - calculate time by mousePosition;
				syncData.Keyframes.Add(new FaceSyncKeyframe(0.5f));
				mSelectedKeyframe = syncData.Keyframes.Count - 1;
				EditorUtility.SetDirty(target);
			}

			for (int i = 0; i < syncData.Keyframes.Count; ++i)
			{
				FaceSyncKeyframe keyframe = syncData.Keyframes[i];
				float x = (width - (border * 2)) * (keyframe.Time / dataDuration);
				string label = keyframe.BlendSet ? keyframe.BlendSet.Label : "!";
				GUI.backgroundColor = i == mSelectedKeyframe ? Color.cyan : Color.grey;
				if (GUI.Button(new Rect(border + x - 10, initY - 20, 20, 18), label))
				{
					mSelectedKeyframe = i;
				}
				GUI.Box(new Rect(border + x, initY, 1, 5), "");
			}


			if (syncData.Sound != null)
			{
				float audioPercentage = syncData.Sound.length / dataDuration;
				GUI.backgroundColor = Color.cyan;
				GUI.Box(new Rect(border, initY, (width - (border * 2)) * audioPercentage, 5), "");
				GUI.backgroundColor = Color.white;

			}

			if (mSelectedKeyframe >= 0)
			{
				GUILayout.BeginArea(new Rect(border, initY + 30, width - (border * 2), 200));
				
				ShowKeyframeData(syncData.Keyframes[mSelectedKeyframe], syncData.Sound.length);
				
				GUILayout.EndArea();
			}

			serializedObject.ApplyModifiedProperties();
		}

		// --------------------------------------------------------------------

		public void ShowKeyframeData(FaceSyncKeyframe keyframe, float maxTime)
		{
			EditorGUI.BeginChangeCheck();
			keyframe.BlendSet = EditorGUILayout.ObjectField(keyframe.BlendSet, typeof(FaceSyncBlendSet), false, null) as FaceSyncBlendSet;
			keyframe.Time = EditorGUILayout.Slider("Time", keyframe.Time, 0, maxTime);

			if (GUILayout.Button("Delete"))
			{
				(target as FaceSyncData).Keyframes.Remove(keyframe);
				mSelectedKeyframe = -1;
			}
			
			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(target);
		}

		// --------------------------------------------------------------------

		private void AutodetectWithRules() // TODO - Move this to an autodetection thing
		{
			FaceSyncData syncData = target as FaceSyncData;
			Dictionary<string, FaceSyncBlendSet> rules = FaceSyncSettings.GetSettings().GetHashedRules();
			float totalDuration = syncData.GetDuration();
			string lowerCaseText = syncData.ReferenceText.ToLower();
			for (int i = 0; i < syncData.ReferenceText.Length; ++i)
			{
				foreach (var rule in rules) {
					if (lowerCaseText.Substring(i).StartsWith(rule.Key.ToLower()))
					{
						FaceSyncKeyframe kf = new FaceSyncKeyframe(((float)i / syncData.ReferenceText.Length) * totalDuration);
						kf.BlendSet = rule.Value;
						syncData.Keyframes.Add(kf);
					}
				}
			}
		}

		// --------------------------------------------------------------------

		public static void PlayClip(AudioClip clip)
		{
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
				"PlayClip",
				BindingFlags.Static | BindingFlags.Public,
				null,
				new System.Type[] {
		 typeof(AudioClip)
			},
			null
			);
			method.Invoke(
				null,
				new object[] {
		 clip
			}
			);
		}
	}
}