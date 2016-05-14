﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using WinterMute;

[CustomEditor(typeof(VRGestureManager)), CanEditMultipleObjects]
public class VRGestureManagerEditor : Editor
{
	static VRGestureManager vrGestureManager;

	// neural net gui helpers
	int selectedNeuralNetIndex = 0;
	string newNeuralNetName;

	// gestures gui helpers
	string editGesturesButtonText;
	bool editGestures = true;

	public enum EditorListOption 
	{
		None = 0,
		ListSize = 1,
		ListLabel = 2,
		ElementLabels = 4,
		Buttons = 8,
		Default = ListSize | ListLabel | ElementLabels,
		NoElementLabels = ListSize | ListLabel,
		ListLabelButtons = ListLabel | Buttons,
		All = Default | Buttons
	}

	private static GUILayoutOption miniButtonWidth = GUILayout.Width(20f);

	private static GUIContent
	useToggleContent = new GUIContent("", "use this gesture"),
	moveButtonContent = new GUIContent("\u21b4", "move down"),
	duplicateButtonContent = new GUIContent("+", "duplicate"),
	deleteButtonContent = new GUIContent("-", "delete"),
	addButtonContent = new GUIContent("+", "add element"),
	neuralNetNoneButtonContent = new GUIContent("+", "click to create a new neural net"),
	trainButtonContent = new GUIContent("TRAIN", "press to train the neural network with the recorded gesture data"),
	detectButtonContent = new GUIContent("DETECT", "press to begin detecting gestures");

	// TEXTURES
	string bg1TexturePath = "Assets/3DGestureTracker/UI/Textures/Resources/bg1.png";
	string bg2TexturePath = "Assets/3DGestureTracker/UI/Textures/Resources/bg2.png";

	Texture2D bg1;
	Texture2D bg2;

    public override void OnInspectorGUI()
    {
		// TEXTURE SETUP
//		bg1 = AssetDatabase.LoadAssetAtPath<Texture2D>(bg1TexturePath);
		bg1 = AssetDatabase.LoadAssetAtPath<Texture2D>("");
//		bg2 = AssetDatabase.LoadAssetAtPath<Texture2D>(bg2TexturePath);
		bg2 = AssetDatabase.LoadAssetAtPath<Texture2D>("");

//        DrawDefaultInspector();
		vrGestureManager = (VRGestureManager)target;
		serializedObject.Update();

		ShowTransforms();

		// NORMAL UI
		if (vrGestureManager.state != VRGestureManagerState.Training) 
		{
			// BACKGROUND / STYLE SETUP
			GUIStyle neuralSectionStyle = new GUIStyle();
			neuralSectionStyle.normal.background = bg1;
			GUIStyle gesturesSectionStyle = new GUIStyle();
			gesturesSectionStyle.normal.background = bg1;
			GUIStyle separatorStyle = new GUIStyle();
			//separatorStyle.normal.background = bg2;

            // SEPARATOR
            GUILayout.BeginHorizontal(separatorStyle);
            EditorGUILayout.Separator(); // a little space between sections
            GUILayout.EndHorizontal();

            // NEURAL NET SECTION
            GUILayout.BeginVertical(neuralSectionStyle);
			ShowNeuralNets();
			GUILayout.EndVertical();

			// SEPARATOR
			GUILayout.BeginVertical(separatorStyle);
			EditorGUILayout.Separator(); // a little space between sections
			GUILayout.EndVertical();
			
			// TRAIN BUTTON
			if (vrGestureManager.readyToTrain && editGestures && neuralNetGUIMode == NeuralNetGUIMode.ShowPopup)
				ShowTrainButton();

			// SEPARATOR
			GUILayout.BeginVertical(separatorStyle);
			EditorGUILayout.Separator(); // a little space between sections
			GUILayout.EndVertical();

			// GESTURE SECTION
			GUILayout.BeginVertical(gesturesSectionStyle);
			// if a neural net is selected
			if (neuralNetGUIMode == NeuralNetGUIMode.ShowPopup)
				ShowGestures();
			GUILayout.EndVertical();

            GUILayout.BeginHorizontal(separatorStyle);
			EditorGUILayout.Separator(); // a little space between sections
            GUILayout.EndHorizontal();

		}
		else if (vrGestureManager.state == VRGestureManagerState.Detecting)// DETECT UI
		{
			ShowDetectMode();
		}
		// TRAINING IS PROCESSING UI
		else if (vrGestureManager.state == VRGestureManagerState.Training)
		{
			ShowTrainingMode();
		}

		serializedObject.ApplyModifiedProperties();
    }

	void ShowTransforms ()
	{
		EditorGUILayout.PropertyField(serializedObject.FindProperty("vrRigAnchors"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("playerHead"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("playerHand"));
	}

	void ShowNeuralNets()
	{
		
		EditorGUILayout.LabelField("NEURAL NETWORK");

		string[] neuralNetsArray = new string[0];
		if (vrGestureManager.neuralNets.Count > 0)
			neuralNetsArray = ConvertStringListPropertyToStringArray("neuralNets");


		// STATE CONTROL
		if (neuralNetGUIMode == NeuralNetGUIMode.EnterNewNetName)
		{
			ShowNeuralNetCreateNewOptions();
		}
		else if (neuralNetsArray.Length == 0) // if the neural nets list is empty show a big + button
		{
			neuralNetGUIMode = NeuralNetGUIMode.None;
		}
		else // draw the popup and little plus and minus buttons
		{
			neuralNetGUIMode = NeuralNetGUIMode.ShowPopup;
		}

		// RENDER
		GUILayout.BeginHorizontal();
		switch (neuralNetGUIMode)
		{
			case (NeuralNetGUIMode.None):
			// PLUS + BUTTON
			if (GUILayout.Button(neuralNetNoneButtonContent))
				{
					newNeuralNetName = "";
					GUI.FocusControl("Clear"); 
					neuralNetGUIMode = NeuralNetGUIMode.EnterNewNetName;
					newNeuralNetName = "";
					GUILayout.EndHorizontal();

				}
			break;
			// NEURAL NET POPUP
			case (NeuralNetGUIMode.ShowPopup):
				ShowNeuralNetPopup(neuralNetsArray);
				GUILayout.EndHorizontal();
				ShowNeuralNetTrainedGestures();
			break;
		}

		// TEMP

		// DEBUG ONLY
//		ShowList(serializedObject.FindProperty("neuralNets"), EditorListOption.ListLabelButtons);

	}

	void ShowNeuralNetTrainedGestures()
	{
		GUIStyle style = EditorStyles.whiteLabel;
		GUILayout.BeginVertical();
			EditorGUILayout.LabelField("TRAINED GESTURES");
		GUILayout.EndVertical();
		GUILayout.BeginVertical(style);
		foreach(string gesture in vrGestureManager.Gestures)
		{
			EditorGUILayout.LabelField(gesture, style);
		}
		GUILayout.EndVertical();
	}

	enum NeuralNetGUIMode { None, EnterNewNetName, ShowPopup };
	NeuralNetGUIMode neuralNetGUIMode;
	int selectedNeuralNetIndexLast;

	void ShowNeuralNetCreateNewOptions ()
	{
		newNeuralNetName = EditorGUILayout.TextField(newNeuralNetName);
		if (GUILayout.Button("Create Network"))
		{
			if (string.IsNullOrEmpty(newNeuralNetName))
			{
				EditorUtility.DisplayDialog("Please give the new neural network a name", " ", "ok");
			}
			else if (vrGestureManager.CheckForDuplicateNeuralNetName(newNeuralNetName))
			{
				EditorUtility.DisplayDialog(
					"The name " + newNeuralNetName + " is already being used, " +
					"please name it something else.", " ", "ok"
				);
			}
			else 
			{
				vrGestureManager.CreateNewNeuralNet(newNeuralNetName);
				selectedNeuralNetIndex = vrGestureManager.neuralNets.IndexOf(newNeuralNetName);
				neuralNetGUIMode = NeuralNetGUIMode.ShowPopup;
			}
		}

	}

    string selectedNeuralNetName = "";

    void ShowNeuralNetPopup (string[] neuralNetsArray)
	{
		selectedNeuralNetIndex = EditorGUILayout.Popup(selectedNeuralNetIndex, neuralNetsArray);
        EventType eventType = Event.current.type;
		if (selectedNeuralNetIndex < neuralNetsArray.Length && eventType == EventType.used)
		{
			selectedNeuralNetName = neuralNetsArray[selectedNeuralNetIndex];

            //Debug.Log(eventType + " " + selectedNeuralNetIndex + " " + neuralNetsArray.Length + " " + selectedNeuralNetName);

            if (neuralNetsArray.Length == 1 || selectedNeuralNetIndex != selectedNeuralNetIndexLast)
			{
				selectedNeuralNetIndexLast = selectedNeuralNetIndex;

				vrGestureManager.SelectNeuralNet(selectedNeuralNetName);
			}
		}
		else
		{
			selectedNeuralNetName = vrGestureManager.currentNeuralNet;
		}

		// + button
		if (GUILayout.Button(duplicateButtonContent, EditorStyles.miniButtonMid, miniButtonWidth))
		{
			newNeuralNetName = "";
			GUI.FocusControl("Clear");
			neuralNetGUIMode = NeuralNetGUIMode.EnterNewNetName;

		}

		// - button
		if (GUILayout.Button(deleteButtonContent, EditorStyles.miniButtonRight, miniButtonWidth))
		{
			if (ShowNeuralNetDeleteDialog(selectedNeuralNetName))
			{
				vrGestureManager.DeleteNeuralNet(selectedNeuralNetName);
				if (vrGestureManager.neuralNets.Count > 0)
					selectedNeuralNetIndex = 0;
			}
		}
	}

	bool ShowNeuralNetDeleteDialog (string neuralNetName)
	{
		return EditorUtility.DisplayDialog("Delete the " + neuralNetName + " neural network?", 
			"This cannot be undone.",
			"ok",
			"cancel"
		);
	}

	void ShowGestures()
	{
//		if (vrGestureManager.gestures.Count == 0)
//			editGestures = true;
		EditorGUILayout.LabelField("RECORDED GESTURES");
		EditorGUI.BeginDisabledGroup(editGestures);
		SerializedProperty gesturesList = serializedObject.FindProperty("gestureBank");
		SerializedProperty size = gesturesList.FindPropertyRelative("Array.size");
		if (size.intValue == 0)
			EditorGUI.EndDisabledGroup();
		ShowList(gesturesList, EditorListOption.Buttons);
		if (size.intValue > 0)
			EditorGUI.EndDisabledGroup();
		if (size.intValue > 0)
			EditGesturesButtonUpdate();
		
	}

	void EditGesturesButtonUpdate ()
	{
		editGesturesButtonText = editGestures ? "Edit Gestures" : editGesturesButtonText = "Save Gestures";
		
		VRGestureManager script = (VRGestureManager)target;
		if (GUILayout.Button(editGesturesButtonText))
		{
			if (editGesturesButtonText == "Edit Gestures")
			{
                if (EditorUtility.DisplayDialog("Are you sure you want to edit gestures?",
                    "If you edit any gestures, you will need to re-train your neural net", "ok"))
                {
                    editGestures = !editGestures;
                }
            }
			if (editGesturesButtonText == "Save Gestures")
			{
				vrGestureManager.SaveGestures();
				editGestures = !editGestures;

			}
		}
	}
	
	void ShowTrainButton()
	{
		if (GUILayout.Button("TRAIN \n" + vrGestureManager.currentNeuralNet, GUILayout.Height(40f)))
		{
			EventType eventType = Event.current.type;
			if (eventType == EventType.used)
			{
				vrGestureManager.BeginTraining(OnFinishedTraining);
			}
		}
	}

	void ShowTrainingMode()
	{
		string trainingInfo = "Training " + vrGestureManager.currentNeuralNet + " is in progress. \n HOLD ON TO YOUR BUTS";

		GUILayout.Label(trainingInfo, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(50f));
		if (GUILayout.Button("QUIT TRAINING"))
		{
			EventType eventType = Event.current.type;
			if (eventType == EventType.used)
			{
				vrGestureManager.EndTraining(OnQuitTraining);
			}
		}
	}

	void ShowDetectButton()
	{
		if (GUILayout.Button(detectButtonContent, GUILayout.Height(40f)))
		{
			EventType eventType = Event.current.type;
			if (eventType == EventType.used)
			{
				vrGestureManager.BeginDetect("I don't know what");
			}
		}
	}

	void ShowDetectMode()
	{

	}

	// callback that VRGestureManager should call upon training finished
	void OnFinishedTraining (string neuralNetName)
	{
//        Debug.Log("on finished training callback for: " + neuralNetName);
	}

	void OnQuitTraining (string neuralNetName)
	{
		
	}

	string[] ConvertStringListPropertyToStringArray (string listName)
	{
		SerializedProperty sp = serializedObject.FindProperty(listName).Copy();
		if (sp.isArray)
		{
			int arrayLength = 0;
			sp.Next(true); // skip generic field
			sp.Next(true); // advance to array size field

			// get array size
			arrayLength = sp.intValue;

			sp.Next(true); // advance to first array index

			// write values to list
			string[] values = new string[arrayLength];
			int lastIndex = arrayLength - 1;
			for (int i = 0; i < arrayLength; i++)
			{
				values[i] = sp.stringValue; // copy the value to the array
				if (i < lastIndex) 
					sp.Next(false); // advance without drilling into children
			}
			return values;
		}
		return null;
	}

	void ShowList (SerializedProperty list, EditorListOption options = EditorListOption.Default)
	{
		bool showListLabel = (options & EditorListOption.ListLabel) != 0;
		bool showListSize = (options & EditorListOption.ListSize) != 0;
		if (showListLabel)
		{
			EditorGUILayout.PropertyField(list);
			EditorGUI.indentLevel += 1;
		}
		if (!showListLabel || list.isExpanded)
		{
			SerializedProperty size = list.FindPropertyRelative("Array.size");
			if (showListSize)
			{
				EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
			}
			if (size.hasMultipleDifferentValues)
			{
				EditorGUILayout.HelpBox("Not showing lists with different sizes.", MessageType.Info);
			}
			else
			{
				ShowElements(list, options);
			}
		}
		if (showListLabel)
			EditorGUI.indentLevel -= 1;
	}

	private static void ShowElements (SerializedProperty list, EditorListOption options)
	{
		if (!list.isArray)
		{
			EditorGUILayout.HelpBox(list.name + " is neither an array nor a list", MessageType.Error);
			return;
		}

		bool showElementLabels = (options & EditorListOption.ElementLabels) != 0;
		bool showButtons = (options & EditorListOption.Buttons) != 0;

		// render the list
		for (int i = 0; i < list.arraySize; i++)
		{

			if (showButtons)
			{
				EditorGUILayout.BeginHorizontal();
			}
			if (showElementLabels)
			{
				EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
			}
			else
			{
				EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);
			}
			if (showButtons)
			{
				ShowButtons(list, i);
				EditorGUILayout.EndHorizontal();
			}
		}
			
		// if the list is empty show the plus + button
		if (showButtons && list.arraySize == 0 && GUILayout.Button(addButtonContent, EditorStyles.miniButton))
		{
            vrGestureManager.CreateGesture("Gesture 1");
		}
	}

	private static void ShowButtons (SerializedProperty list, int index)
	{
		// use toggle
//		if (GUILayout.Toggle(false, useToggleContent, miniButtonWidth))
//		{
////			Debug.Log("do ssomething toggle");
//		}
		// plus button
		if (GUILayout.Button(duplicateButtonContent, EditorStyles.miniButtonMid, miniButtonWidth))
		{
            //list.InsertArrayElementAtIndex(index);
            int size = list.arraySize + 1;
            vrGestureManager.CreateGesture("Gesture " + size);
		}
		// minus button
		if (GUILayout.Button(deleteButtonContent, EditorStyles.miniButtonRight, miniButtonWidth))
		{
            // new way to delete using vrGestureManager directly
            string gestureName = list.GetArrayElementAtIndex(index).stringValue;
            vrGestureManager.DeleteGesture(gestureName);

            // old way to delete from property
            //int oldSize = list.arraySize;
            //list.DeleteArrayElementAtIndex(index);
            //if (list.arraySize == oldSize)
            //    list.DeleteArrayElementAtIndex(index);
        }
	}

		
}