﻿using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;

using NodeEditorFramework;
using NodeEditorFramework.Utilities;

using GenericMenu = UnityEditor.GenericMenu;

namespace NodeEditorFramework.Standard
{
	public class NodeEditorWindow : EditorWindow 
	{
		// Information about current instance
		private static NodeEditorWindow _editor;
		public static NodeEditorWindow editor { get { AssureEditor(); return _editor; } }
		public static void AssureEditor() { if (_editor == null) OpenNodeEditor(); }

		// Opened Canvas
		public NodeEditorUserCache canvasCache;

		// GUI
		private string sceneCanvasName = "";
		private Rect loadSceneUIPos;
		private Rect createCanvasUIPos;
		private Rect convertCanvasUIPos;
		private int sideWindowWidth = 400;
		private int toolbarHeight = 17;

		private Rect modalWindowRect = new Rect(20, 50, 250, 100);

		private bool showSideWindow;
		private bool showModalPanel;

		public Rect sideWindowRect { get { return new Rect (position.width - sideWindowWidth, toolbarHeight, sideWindowWidth, position.height); } }
		public Rect canvasWindowRect { get { return new Rect(0, toolbarHeight, position.width - (showSideWindow? sideWindowWidth : 0), position.height - toolbarHeight); } }

		#region General 

		/// <summary>
		/// Opens the Node Editor window and loads the last session
		/// </summary>
		[MenuItem("Window/Node Editor")]
		public static NodeEditorWindow OpenNodeEditor () 
		{
			_editor = GetWindow<NodeEditorWindow>();
			_editor.minSize = new Vector2(400, 200);
			NodeEditor.ReInit (false);

			Texture iconTexture = ResourceManager.LoadTexture (EditorGUIUtility.isProSkin? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			_editor.titleContent = new GUIContent ("Node Editor", iconTexture);

			return _editor;
		}

		[UnityEditor.Callbacks.OnOpenAsset(1)]
		private static bool AutoOpenCanvas(int instanceID, int line)
		{
			if (Selection.activeObject != null && Selection.activeObject is NodeCanvas)
			{
				string NodeCanvasPath = AssetDatabase.GetAssetPath(instanceID);
				NodeEditorWindow.OpenNodeEditor().canvasCache.LoadNodeCanvas(NodeCanvasPath);
				return true;
			}
			return false;
		}

		private void OnEnable()
		{
			_editor = this;
			NodeEditor.checkInit(false);

			NodeEditor.ClientRepaints -= Repaint;
			NodeEditor.ClientRepaints += Repaint;

			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justLeftPlayMode += NormalReInit;
			// Here, both justLeftPlayMode and justOpenedNewScene have to act because of timing
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene += NormalReInit;

			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			// Setup Cache
			canvasCache = new NodeEditorUserCache(Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject (this))));
			//canvasCache.defaultNodeCanvasType = typeof(GraphCanvasType);
			canvasCache.SetupCacheEvents();
			if (canvasCache.nodeCanvas.GetType () == typeof(NodeCanvas))
				ShowNotification(new GUIContent("The Canvas has no specific type. Please use the convert button to assign a type and re-save the canvas!"));
			canvasCache.nodeCanvas.Validate (false);
		}

		private void NormalReInit()
		{
			NodeEditor.ReInit(false);
			canvasCache.nodeCanvas.Validate (false);
		}

		private void OnDestroy()
		{
			NodeEditor.ClientRepaints -= Repaint;

			EditorLoadingControl.justLeftPlayMode -= NormalReInit;
			EditorLoadingControl.justOpenedNewScene -= NormalReInit;

			SceneView.onSceneGUIDelegate -= OnSceneGUI;

			// Clear Cache
			canvasCache.ClearCacheEvents ();
		}

		public void OnLostFocus () 
		{ // Save any changes made while focussing this window
			// Will also save before possible assembly reload, scene switch, etc. because these require focussing of a different window
			canvasCache.SaveCache ();
		}

		#endregion

		#region GUI

		private void OnSceneGUI(SceneView sceneview)
		{
			DrawSceneGUI();
		}

		private void DrawSceneGUI()
		{
			if (canvasCache.editorState.selectedNode != null)
				canvasCache.editorState.selectedNode.OnSceneGUI();
			SceneView.lastActiveSceneView.Repaint();
		}

		private void DoModalWindow(int unusedWindowID)
		{
			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			bool overwrite = NodeEditorSaveManager.HasSceneSave (sceneCanvasName);
			if (overwrite)
				GUILayout.Label (new GUIContent ("!!!", "A canvas with the specified name already exists. It will be overwritten!"), GUILayout.ExpandWidth (false));
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			if (GUILayout.Button("Cancel"))
				showModalPanel = false;
			if (GUILayout.Button (new GUIContent (overwrite? "Overwrite" : "Save", "Save the canvas to the Scene")))
			{
				showModalPanel = false;
				canvasCache.SaveSceneNodeCanvas (sceneCanvasName);
			}
			GUILayout.EndHorizontal ();
		}

		private void OnGUI()
		{
			// Initiation
			NodeEditor.checkInit(true);
			if (NodeEditor.InitiationError || canvasCache == null)
			{
				GUILayout.Label("Node Editor Initiation failed! Check console for more information!");
				return;
			}
			AssureEditor ();
			canvasCache.AssureCanvas ();

			// Specify the Canvas rect in the EditorState, currently disabled for dynamic sidebar resizing
			// canvasCache.editorState.canvasRect = canvasWindowRect;
			// If you want to use GetRect:
//			Rect canvasRect = GUILayoutUtility.GetRect (600, 600);
//			if (Event.current.type != EventType.Layout)
//				mainEditorState.canvasRect = canvasRect;
			NodeEditorGUI.StartNodeGUI ("NodeEditorWindow", true);

			canvasCache.editorState.canvasRect = canvasWindowRect;

			// Perform drawing with error-handling
			try
			{
				NodeEditor.DrawCanvas (canvasCache.nodeCanvas, canvasCache.editorState);
			}
			catch (UnityException e)
			{ // on exceptions in drawing flush the canvas to avoid locking the ui.
				canvasCache.NewNodeCanvas ();
				NodeEditor.ReInit (true);
				Debug.LogError ("Unloaded Canvas due to an exception during the drawing phase!");
				Debug.LogException (e);
			}

			// Draw Toolbar
			DrawToolbarGUI();

			if (showSideWindow)
			{ // Draw Side Window
				sideWindowWidth = Math.Min(600, Math.Max(200, (int)(position.width / 5)));
				GUILayout.BeginArea(sideWindowRect, GUI.skin.box);
				DrawSideWindow();
				GUILayout.EndArea();
			}

			if (showModalPanel)
			{
				BeginWindows();
				modalWindowRect = GUILayout.Window(0, modalWindowRect, DoModalWindow, "Save to Scene");
				EndWindows();
			}

			NodeEditorGUI.EndNodeGUI();
		}

		private void DrawToolbarGUI()
		{
			EditorGUILayout.BeginHorizontal("Toolbar");
			GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);

			if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(50)))
			{
				GenericMenu menu = new GenericMenu();

				// Canvas creation
				foreach (System.Collections.Generic.KeyValuePair<Type, NodeCanvasTypeData> data in NodeCanvasManager.CanvasTypes)
					menu.AddItem(new GUIContent("New Canvas/" + data.Value.DisplayString), false, CreateCanvasCallback, data.Key);
//				menu.AddItem(new GUIContent("New Standard Canvas"), false, CreateCanvasCallback, null);
				menu.AddSeparator("");              

				// Scene Saving
				menu.AddItem(new GUIContent("Load Canvas", "Loads an asset canvas"), false, LoadCanvas);
				menu.AddItem(new GUIContent("Reload Canvas", "Restores the current canvas to when it has been last saved."), false, ReloadCanvas);
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Save Canvas"), false, SaveCanvas);
				menu.AddItem(new GUIContent("Save Canvas As"), false, SaveCanvasAs);

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Save Xml"), false, SaveXml);
                menu.AddItem(new GUIContent("Load Xml"), false, LoadXml);
                menu.AddSeparator("");
                

				// Scene Saving
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves())
				{
					if (sceneSave.ToLower () != "lastsession")
						menu.AddItem(new GUIContent("Load Canvas from Scene/" + sceneSave), false, LoadSceneCanvasCallback, sceneSave);       
				}
				menu.AddItem( new GUIContent("Save Canvas to Scene"), false, () => showModalPanel = true);

				menu.DropDown (new Rect (5, toolbarHeight, 0, 0));


            }

            if (GUILayout.Button("Debug", EditorStyles.toolbarDropDown, GUILayout.Width(50)))
			{
				GenericMenu menu = new GenericMenu();

				// Toggles side panel
				menu.AddItem(new GUIContent("Sidebar"), showSideWindow, () => showSideWindow = !showSideWindow);

				menu.DropDown (new Rect (55, toolbarHeight, 0, 0));
			}

			GUILayout.Space(10);
			GUILayout.FlexibleSpace();

			GUILayout.Label (new GUIContent ("" + canvasCache.nodeCanvas.saveName + " (" + (canvasCache.nodeCanvas.livesInScene? "Scene Save" : "Asset Save") + ")", "Opened Canvas path: " + canvasCache.nodeCanvas.savePath), "ToolbarButton");
			GUILayout.Label ("Type: " + canvasCache.typeData.DisplayString /*+ "/" + canvasCache.nodeCanvas.GetType ().Name*/, "ToolbarButton");

			GUI.backgroundColor = new Color(1, 0.3f, 0.3f, 1);
			if (GUILayout.Button("Force Re-init", EditorStyles.toolbarButton, GUILayout.Width(80)))
				NodeEditor.ReInit (true);
			GUI.backgroundColor = Color.white;

			EditorGUILayout.EndHorizontal();
		}

		private void DrawSideWindow()
		{
			GUILayout.Label (new GUIContent ("" + canvasCache.nodeCanvas.saveName + " (" + (canvasCache.nodeCanvas.livesInScene? "Scene Save" : "Asset Save") + ")", "Opened Canvas path: " + canvasCache.nodeCanvas.savePath), NodeEditorGUI.nodeLabelBold);
			GUILayout.Label ("Type: " + canvasCache.typeData.DisplayString + "/" + canvasCache.nodeCanvas.GetType ().Name + "");

//			EditorGUILayout.ObjectField ("Loaded Canvas", canvasCache.nodeCanvas, typeof(NodeCanvas), false);
//			EditorGUILayout.ObjectField ("Loaded State", canvasCache.editorState, typeof(NodeEditorState), false);

			if (GUILayout.Button(new GUIContent("New Canvas", "Loads an Specified Empty CanvasType")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, canvasCache.NewNodeCanvas);
				menu.Show(createCanvasUIPos.position, createCanvasUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				createCanvasUIPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}
			if (canvasCache.nodeCanvas.GetType () == typeof(NodeCanvas) && GUILayout.Button(new GUIContent("Convert Canvas", "Converts the current canvas to a new type.")))
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				NodeCanvasManager.FillCanvasTypeMenu(ref menu, canvasCache.ConvertCanvasType);
				menu.Show(convertCanvasUIPos.position, convertCanvasUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect();
				convertCanvasUIPos = new Rect(popupPos.x + 2, popupPos.yMax + 2, popupPos.width - 4, 0);
			}

			if (GUILayout.Button(new GUIContent("Save Canvas", "Save the Canvas to the load location")))
			{
				SaveCanvas();
			}

			GUILayout.Space(6);

			GUILayout.Label ("Asset Saving", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button(new GUIContent("Save Canvas As", "Save the canvas as an asset")))
			{
				SaveCanvasAs();
			}

			if (GUILayout.Button(new GUIContent("Load Canvas", "Load the Canvas from an asset")))
			{
				string panelPath = NodeEditor.editorPath + "Resources/Saves/";
				if (canvasCache.nodeCanvas != null && !string.IsNullOrEmpty(canvasCache.nodeCanvas.savePath))
					panelPath = canvasCache.nodeCanvas.savePath;
				string path = EditorUtility.OpenFilePanel("Load Node Canvas", panelPath, "asset");
				if (!path.Contains(Application.dataPath))
				{
					if (!string.IsNullOrEmpty(path))
						ShowNotification(new GUIContent("You should select an asset inside your project folder!"));
				}
				else
					canvasCache.LoadNodeCanvas (path);
				if (canvasCache.nodeCanvas.GetType () == typeof(NodeCanvas))
					ShowNotification(new GUIContent("The Canvas has no specific type. Please use the convert button to assign a type and re-save the canvas!"));
			}

			//GUILayout.Space(6);
			GUILayout.Label ("Scene Saving", NodeEditorGUI.nodeLabel);

			GUILayout.BeginHorizontal ();
			sceneCanvasName = GUILayout.TextField (sceneCanvasName, GUILayout.ExpandWidth (true));
			if (GUILayout.Button (new GUIContent ("Save to Scene", "Save the canvas to the Scene"), GUILayout.ExpandWidth (false)))
				canvasCache.SaveSceneNodeCanvas (sceneCanvasName);
			GUILayout.EndHorizontal ();

			if (GUILayout.Button (new GUIContent ("Load from Scene", "Load the canvas from the Scene"))) 
			{
				NodeEditorFramework.Utilities.GenericMenu menu = new NodeEditorFramework.Utilities.GenericMenu();
				foreach (string sceneSave in NodeEditorSaveManager.GetSceneSaves())
					menu.AddItem(new GUIContent(sceneSave), false, LoadSceneCanvasCallback, (object)sceneSave);
				menu.Show (loadSceneUIPos.position, loadSceneUIPos.width);
			}
			if (Event.current.type == EventType.Repaint)
			{
				Rect popupPos = GUILayoutUtility.GetLastRect ();
				loadSceneUIPos = new Rect (popupPos.x+2, popupPos.yMax+2, popupPos.width-4, 0);
			}

			//GUILayout.Space (6);
			GUILayout.Label ("Utility/Debug", NodeEditorGUI.nodeLabel);

			if (GUILayout.Button (new GUIContent ("Recalculate All", "Initiates complete recalculate. Usually does not need to be triggered manually.")))
				canvasCache.nodeCanvas.TraverseAll ();

			if (GUILayout.Button ("Force Re-Init"))
				NodeEditor.ReInit (true);

			NodeEditorGUI.knobSize = EditorGUILayout.IntSlider (new GUIContent ("Handle Size", "The size of the Node Input/Output handles"), NodeEditorGUI.knobSize, 12, 20);
			//canvasCache.editorState.zoom = EditorGUILayout.Slider (new GUIContent ("Zoom", "Use the Mousewheel. Seriously."), canvasCache.editorState.zoom, 0.6f, 4);
			NodeEditorUserCache.cacheIntervalSec = EditorGUILayout.IntSlider (new GUIContent ("Cache Interval (Sec)", "The interval in seconds the canvas is temporarily saved into the cache as a precaution for crashes."), NodeEditorUserCache.cacheIntervalSec, 30, 300);

//			NodeEditorGUI.curveBaseDirection = EditorGUILayout.FloatField ("Curve Base Dir", NodeEditorGUI.curveBaseDirection);
//			NodeEditorGUI.curveBaseStart = EditorGUILayout.FloatField ("Curve Base Start", NodeEditorGUI.curveBaseStart);
//			NodeEditorGUI.curveDirectionScale = EditorGUILayout.FloatField ("Curve Dir Scale", NodeEditorGUI.curveDirectionScale);

			if (canvasCache.editorState.selectedNode != null && Event.current.type != EventType.Ignore)
				canvasCache.editorState.selectedNode.DrawNodePropertyEditor();
		}

		#endregion

		#region Menu Callbacks

		private void CreateCanvasCallback(object userdata)
		{
			editor.canvasCache.NewNodeCanvas((Type)userdata);
		}	

		private void LoadCanvas()
		{
			string path = EditorUtility.OpenFilePanel("Load Node Canvas", NodeEditor.editorPath + "Resources/Saves/", "asset");
			if (!path.Contains(Application.dataPath))
			{
				if (!string.IsNullOrEmpty(path))
					ShowNotification(new GUIContent("You should select an asset inside your project folder!"));
			}
			else
				canvasCache.LoadNodeCanvas (path);
		}

		private void ReloadCanvas()
		{
			string path = canvasCache.nodeCanvas.savePath;
			if (!string.IsNullOrEmpty (path))
			{
				if (path.StartsWith ("SCENE/"))
					canvasCache.LoadSceneNodeCanvas (path.Substring (6));
				else
					canvasCache.LoadNodeCanvas (path);
				ShowNotification (new GUIContent ("Canvas Reloaded!"));
			}
			else
				ShowNotification (new GUIContent ("Cannot reload canvas as it has not been saved yet!"));
		}

		private void SaveCanvas()
		{
			string path = canvasCache.nodeCanvas.savePath;
			if (!string.IsNullOrEmpty (path))
			{
				if (path.StartsWith ("SCENE/"))
					canvasCache.SaveSceneNodeCanvas (path.Substring (6));
				else
					canvasCache.SaveNodeCanvas (path);
				ShowNotification (new GUIContent ("Canvas Saved!"));
			}
			else
				ShowNotification (new GUIContent ("No save location found. Use 'Save As'!"));
		}

		private void SaveCanvasAs()
		{
			string panelPath = NodeEditor.editorPath + "Resources/Saves/";
			if (canvasCache.nodeCanvas != null && !string.IsNullOrEmpty(canvasCache.nodeCanvas.savePath))
				panelPath = canvasCache.nodeCanvas.savePath;
			string path = EditorUtility.SaveFilePanelInProject ("Save Node Canvas", "Node Canvas", "asset", "", panelPath);
			if (!string.IsNullOrEmpty (path))
				canvasCache.SaveNodeCanvas (path);
		}

        private void SaveXml()
        {
            canvasCache.SaveXml(Application.dataPath + "/hello.xml");
            //NodeEditorSaveManager.SaveXml(Application.dataPath + "/hello.xml", ref canvasCache.nodeCanvas);
        }

        private void LoadXml()
        {
            canvasCache.LoadXml(Application.dataPath + "/hello.xml");
            //NodeEditorSaveManager.LoadXml(Application.dataPath + "/hello.xml", ref canvasCache.nodeCanvas);
        }

		public void LoadSceneCanvasCallback (object canvas) 
		{
			canvasCache.LoadSceneNodeCanvas ((string)canvas);
		}

		#endregion
	}
}