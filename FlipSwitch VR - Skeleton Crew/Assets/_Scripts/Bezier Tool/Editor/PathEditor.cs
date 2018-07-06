using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace LivingValkyrie.Util {
	/// <summary>
	/// Author: Matt Gipson
	/// Contact: Deadwynn@gmail.com
	/// Domain: www.livingvalkyrie.net
	/// 
	/// Description: PathEditor
	/// </summary>
	[CustomEditor(typeof(PathCreator))]
	public class PathEditor : Editor {
		#region Fields

		PathCreator creator;
		Path path {
			get { return creator.path; }
		}
		int selectedSegmentIndex = -1;
		public float segmentSelectDstThreshold = .1f;

		#endregion

		void HandleInput() {
			Event guiEvent = Event.current;
			Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

			//if user Shift left clicks
			if (guiEvent.ShiftLeftClick()) {
				if (selectedSegmentIndex != -1) {
					Undo.RecordObject(creator, "add segment");
					path.SplitSegment(mousePos, selectedSegmentIndex);
				} else if (!path.IsClosed) {
					Undo.RecordObject(creator, "add segment");
					path.AddSegment(mousePos);
				}
			}

			//right click
			if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1) {
				float minDstToAnchor = creator.anchorDiameter * 0.5f;
				int closestAnchorIndex = -1;

				for (int i = 0; i < path.NumPoints; i += 3) {
					float dst = Vector2.Distance(mousePos, path[i]);
					if (dst < minDstToAnchor) {
						minDstToAnchor = dst;
						closestAnchorIndex = i;
					}
				}

				if (closestAnchorIndex != -1) {
					Undo.RecordObject(creator, "delete segment");
					path.DeleteSegment(closestAnchorIndex);
				}
			}

			if (guiEvent.type == EventType.MouseMove) {
				float minDstToSegment = segmentSelectDstThreshold;
				int newSelectedSegmentIndex = -1;

				for (int i = 0; i < path.NumSegments; i++) {
					Vector2[] points = path.GetPointsInSegment(i);
					float dst = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
					if (dst < minDstToSegment) {
						minDstToSegment = dst;
						newSelectedSegmentIndex = i;
					}
				}

				if (newSelectedSegmentIndex != selectedSegmentIndex) {
					selectedSegmentIndex = newSelectedSegmentIndex;
					HandleUtility.Repaint();
				}
			}

			//todo move entire path when moving gameobject
		}

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			EditorGUI.BeginChangeCheck();

			if (GUILayout.Button("Create New")) {
				Undo.RecordObject(creator, "new path");
				creator.CreatePath();
			}

			bool isClosed = GUILayout.Toggle(path.IsClosed, "Closed");
			if (isClosed != path.IsClosed) {
				Undo.RecordObject(creator, "toggle closed");

				path.IsClosed = isClosed;
			}

			bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Atuo Set Control Points");
			if (autoSetControlPoints != path.AutoSetControlPoints) {
				Undo.RecordObject(creator, "Toggle autoset control points");
				path.AutoSetControlPoints = autoSetControlPoints;
			}

			if (EditorGUI.EndChangeCheck()) {
				SceneView.RepaintAll();
			}
		}

		void OnSceneGUI() {
			HandleInput();
			Draw();
		}

		void Draw() {
			for (int i = 0; i < path.NumSegments; i++) {
				Vector2[] points = path.GetPointsInSegment(i);

				//control point, anchor
				if (creator.displayControlPoints) {
					Handles.color = creator.controlLineColor;
					Handles.DrawLine(points[1], points[0]);
					Handles.DrawLine(points[2], points[3]);
				}

				Color segmentCol = (i == selectedSegmentIndex && Event.current.shift) ? creator.selectedSegmentColor : creator.segmentColor;
				Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, creator.controlLineWidth);
			}

			for (int i = 0; i < path.NumPoints; i++) {
				if (i % 3 == 0 || creator.displayControlPoints) {
					Handles.color = (i % 3 == 0) ? creator.anchorColor : creator.controlColor;
					float handleSize = (i % 3 == 0) ? creator.anchorDiameter : creator.controlDiameter;
					Vector2 newPos = Handles.FreeMoveHandle(path[i], Quaternion.identity, handleSize, Vector2.zero, Handles.CylinderHandleCap);

					if (path[i] != newPos) {
						if (Event.current.control) {
							Undo.RecordObject(creator, "move point");
							path.MovePoint(i, newPos, false);
						} else {
							Undo.RecordObject(creator, "move point");
							path.MovePoint(i, newPos);
						}
					}
				}
			}
		}

		//todo swap to gizmoes to draw?

		void OnEnable() {
			creator = (PathCreator)target;
			if (creator.path == null) {
				creator.CreatePath();
			}
		}

	}
}