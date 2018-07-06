using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LivingValkyrie.Util {

	/// <summary>
	/// Author: Matt Gipson
	/// Contact: Deadwynn@gmail.com
	/// Domain: www.livingvalkyrie.net
	/// 
	/// Description: Path
	/// </summary>
	[System.Serializable]
	public class Path {
		#region Fields

		[SerializeField, HideInInspector]
		List<Vector2> points;
		[SerializeField, HideInInspector]
		bool isClosed, autoSetControlPoints;

		public bool AutoSetControlPoints {
			get { return autoSetControlPoints; }
			set {
				if (autoSetControlPoints != value) {
					autoSetControlPoints = value;
					if (autoSetControlPoints) {
						AutoSetAllControlPoints();
					}
				}
			}
		}

		#endregion

		public Path(Vector2 center) {
			points = new List<Vector2> {
				center + Vector2.left,
				center + (Vector2.left + Vector2.up) * 0.5f,
				center + (Vector2.right + Vector2.down) * 0.5f,
				center + Vector2.right
			};
		}

		public bool IsClosed {
			get { return isClosed; }
			set {
				if (isClosed != value) {
					isClosed = value;

					if (isClosed) {
						points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
						points.Add(points[0] * 2 - points[1]);
						if (autoSetControlPoints) {
							AutoSetAnchorControlPoints(0);
							AutoSetAnchorControlPoints(points.Count - 3);
						}
					} else {
						points.RemoveRange(points.Count - 2, 2);
						if (autoSetControlPoints) {
							AutoSetStartAndEndControls();
						}
					}
				}
			}
		}

		public Vector2 this[int i] {
			get { return points[i]; }
		}

		public int NumPoints {
			get { return points.Count; }
		}

		public int NumSegments {
			get { return points.Count / 3; }
		}

		/// <summary>
		/// Adds a segment to the line with [anchorPos] as the point. creates control points automatically.
		/// </summary>
		/// <param name="anchorPos">The anchor position.</param>
		public void AddSegment(Vector2 anchorPos) {
			//the point to create stright line from existing control point
			points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);

			//half way between ^ and anchor nwPos for control point
			points.Add((points[points.Count - 1] + anchorPos) * .5f);

			//actual anchor nwPos
			points.Add(anchorPos);

			if (autoSetControlPoints) {
				AutoSetAllAffectedControlPoints(points.Count - 1);
			}

			//todo add logic to add points to the front of the line?
		}

		public void SplitSegment(Vector2 anchorPos, int segmentIndex) {
			points.InsertRange(segmentIndex * 3 + 2, new Vector2[] {Vector2.zero, anchorPos, Vector2.zero});
			if (autoSetControlPoints) {
				AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
			} else {
				AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
			}
		}

		/// <summary>
		/// Gets the points in segment.
		/// </summary>
		/// <param name="i">The index of the segment</param>
		/// <returns>the first and last anchor aswell as both control points (anchor, control, control, anchor)</returns>
		public Vector2[] GetPointsInSegment(int i) {
			return new Vector2[] {
				points[i * 3],
				points[i * 3 + 1],
				points[i * 3 + 2],
				points[LoopIndex(i * 3 + 3)]
			};
		}

		/// <summary>
		/// Moves the point.
		/// </summary>
		/// <param name="i">The i.</param>
		/// <param name="newPos">The new position.</param>
		/// <param name="moveCorrespondingControlpoint"></param>
		public void MovePoint(int i, Vector2 newPos, bool moveCorrespondingControlpoint = true) {
			Vector2 deltaMove = newPos - points[i];

			if (i % 3 == 0 || !autoSetControlPoints) {
				points[i] = newPos;

				if (autoSetControlPoints) {
					AutoSetAllAffectedControlPoints(i);
				} else {
					if (i % 3 == 0) {
						//achor point
						if (i + 1 < points.Count || isClosed) {
							points[LoopIndex(i + 1)] += deltaMove;
						}
						if (i - 1 >= 0 || isClosed) {
							points[LoopIndex(i - 1)] += deltaMove;
						}
					} else {
						//control point
						bool nextPointIsAnchor = (i + 1) % 3 == 0;
						int correspondingControlpoint = (nextPointIsAnchor) ? i + 2 : i - 2;
						int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;

						if (moveCorrespondingControlpoint) {
							if (correspondingControlpoint >= 0 && correspondingControlpoint < points.Count || isClosed) {
								float dist = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlpoint)]).magnitude;
								Vector2 dir = (points[LoopIndex(anchorIndex)] - newPos).normalized;
								points[LoopIndex(correspondingControlpoint)] = points[LoopIndex(anchorIndex)] + dir * dist;
							}
						}
					}
				}
			}
		}

		void AutoSetAllAffectedControlPoints(int updatedAnchorIndex) {
			for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3) {
				if (i >= 0 && i < points.Count || isClosed) {
					AutoSetAnchorControlPoints(LoopIndex(i));
				}
			}

			AutoSetStartAndEndControls();
		}

		void AutoSetAllControlPoints() {
			for (int i = 0; i < points.Count; i += 3) {
				AutoSetAnchorControlPoints(i);
			}
			AutoSetStartAndEndControls();
		}

		void AutoSetAnchorControlPoints(int anchorIndex) {
			Vector2 anchorPos = points[anchorIndex];
			Vector2 dir = Vector2.zero;
			float[] neighbourDistances = new float[2];

			if (anchorIndex - 3 >= 0 || isClosed) {
				Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
				dir += offset.normalized;
				neighbourDistances[0] = offset.magnitude;
			}
			if (anchorIndex + 3 >= 0 || isClosed) {
				Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
				dir -= offset.normalized;
				neighbourDistances[1] = -offset.magnitude;
			}

			dir.Normalize();

			for (int i = 0; i < 2; i++) {
				int controlIndex = anchorIndex + i * 2 - 1;
				if (controlIndex >= 0 && controlIndex < points.Count || isClosed) {
					points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f;
				}
			}
		}

		void AutoSetStartAndEndControls() {
			if (!isClosed) {
				points[1] = (points[0] + points[2]) * .5f;
				points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;
			}
		}

		int LoopIndex(int i) {
			return (i + points.Count) % points.Count;
		}

		public void DeleteSegment(int anchorIndex) {
			if (NumSegments > 2 || !isClosed && NumSegments > 1) {
				if (anchorIndex == 0) {
					//first anchor
					if (isClosed) {
						//shift last point to avoid any weird stuff
						points[points.Count - 1] = points[2];
					}
					points.RemoveRange(0, 3);
				} else if (anchorIndex == points.Count - 1 && isClosed) {
					//last anchor
					points.RemoveRange(anchorIndex - 2, 3);
				} else {
					points.RemoveRange(anchorIndex - 1, 3);
				}
			}
		}

		public Vector2[] CalculateEvenlySpacedPoints(float spacing, float resolution = 1) {
			List<Vector2> evenlySpacedPoints = new List<Vector2>();

			evenlySpacedPoints.Add(points[0]);
			Vector2 previousPoint = points[0];
			float dstSinceLastPoint = 0;

			for (int segmentIndex = 0; segmentIndex < NumSegments; segmentIndex++) {
				Vector2[] p = GetPointsInSegment(segmentIndex);
				float controlNetLenght = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
				float estimatedCurveLength = Vector2.Distance(p[0], p[3]) + controlNetLenght / 2f;
				int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);

				float t = 0;
				while (t <= 1) {
					t += 1f / divisions;
					Vector2 pointOnCurve = HelperFunctions.CubicCurve(p[0], p[1], p[2], p[3], t);
					dstSinceLastPoint += Vector2.Distance(previousPoint, pointOnCurve);

					while (dstSinceLastPoint >= spacing) {
						float overShootDst = dstSinceLastPoint - spacing;
						Vector2 newEvenlySpacedPoint = pointOnCurve + (previousPoint - pointOnCurve).normalized * overShootDst;
						evenlySpacedPoints.Add(newEvenlySpacedPoint);
						dstSinceLastPoint = overShootDst;
						previousPoint = newEvenlySpacedPoint;
					}

					previousPoint = pointOnCurve;
				}
			}

			return evenlySpacedPoints.ToArray();
		}

	}
}