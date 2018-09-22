using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Author: Matt Gipson
/// Contact: Deadwynn@gmail.com
/// Domain: www.livingvalkyrie.net
/// 
/// Description: HelperFunctions
/// </summary>
public static class HelperFunctions {
	#region Vector2 helper functions

	/// <summary>
	/// lerps the given vector2s.
	/// </summary>
	/// <param name="a">From.</param>
	/// <param name="b">To.</param>
	/// <param name="t">The point of interpulation (0-1)</param>
	/// <returns>the interpulated point at t</returns>
	public static Vector2 Lerp(Vector2 a, Vector2 b, float t) {
		return a + (b - a) * t;
	}

	/// <summary>
	/// calculates the point of a quadratic curve at t.
	/// </summary>
	/// <param name="a">From.</param>
	/// <param name="b">The control point.</param>
	/// <param name="c">To.</param>
	/// <param name="t">The point of interpulation (0-1)</param>
	/// <returns></returns>
	public static Vector2 QuadraticCurve(Vector2 a, Vector2 b, Vector2 c, float t) {
		Vector2 p0 = Lerp(a, b, t);
		Vector2 p1 = Lerp(b, c, t);

		return Lerp(p0, p1, t);
	}

	/// <summary>
	/// calculates the point of a cubic curve at t.
	/// </summary>
	/// <param name="a">From.</param>
	/// <param name="b">The control point0.</param>
	/// <param name="c">The control point1.</param>
	/// <param name="d">To.</param>
	/// <param name="t">The point of interpulation (0-1)</param>
	/// <returns></returns>
	public static Vector2 CubicCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) {
		Vector2 p0 = QuadraticCurve(a, b, c, t);
		Vector2 p1 = QuadraticCurve(b, c, d, t);
		return Lerp(p0, p1, t);
	}



	#endregion

	#region Event helper functions

	public static bool ShiftLeftClick(this Event guiEvent) {
		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift) {
			return true;
		}
		return false;
	}

	public static bool ShiftRightClick(this Event guiEvent) {
		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1 && guiEvent.shift) {
			return true;
		}
		return false;
	}

	public static bool ShiftMiddleClick(this Event guiEvent) {
		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 2 && guiEvent.shift) {
			return true;
		}
		return false;
	}

    #endregion

    #region Dictionary helper functions

    public static rt TryGetValue<t, rt>(this Dictionary<t, rt> dictionary, t key) {
        rt value;

        if (dictionary.TryGetValue(key, out value)) {
            return value;
        } else {
            return default(rt);
        }
        
    }

    #endregion

    #region List Helper Functions

    public static int GetCountOfEntriesForType<T>(this List<T> list, T entryType) where T : IComparable {
        int toReturn = 0;

        for (int i = 0; i < list.Count; i++) {
            if (list[i].CompareTo( entryType) == 0) {
                //same value
                toReturn++;
            }
        }


        return toReturn;
    }

    #endregion
}