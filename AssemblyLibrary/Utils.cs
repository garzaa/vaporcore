using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class Utils {
    static System.Random random = new System.Random();

	public static void WaitAndExecute(this MonoBehaviour mono, Action action, float seconds) {
		mono.StartCoroutine(_WaitAndExecute(action, seconds));
	}

	static IEnumerator _WaitAndExecute(Action action, float seconds) {
		yield return new WaitForSeconds(seconds);
		action();
	}

	public static T RandomDictValue<U, T>(IDictionary<U, T> dict) {
		List<T> values = Enumerable.ToList(dict.Values);
		return values[random.Next(values.Count)];
	}

	public static Vector3 Round(this Vector3 vector3, int decimalPlaces = 2) {
        float multiplier = 1;
        for (int i = 0; i < decimalPlaces; i++) {
            multiplier *= 10f;
        }
        return new Vector3(
            Mathf.Round(vector3.x * multiplier) / multiplier,
            Mathf.Round(vector3.y * multiplier) / multiplier,
            Mathf.Round(vector3.z * multiplier) / multiplier);
    }

    public static Vector3 Floor(this Vector3 vector3, int decimalPlaces = 2) {
        float multiplier = 1;
        for (int i = 0; i < decimalPlaces; i++) {
            multiplier *= 10f;
        }
        return new Vector3(
            Mathf.Floor(vector3.x * multiplier) / multiplier,
            Mathf.Floor(vector3.y * multiplier) / multiplier,
            Mathf.Floor(vector3.z * multiplier) / multiplier);
    }

    public static Vector2 GetSnapToPositionToBringChildIntoView(this ScrollRect instance, RectTransform child) {
        Canvas.ForceUpdateCanvases();
        Vector2 viewportLocalPosition = instance.viewport.localPosition;
        Vector2 childLocalPosition   = child.localPosition;
        Vector2 result = new Vector2(
            0 - (viewportLocalPosition.x + childLocalPosition.x),
            0 - (viewportLocalPosition.y + childLocalPosition.y)
        );
        Canvas.ForceUpdateCanvases();
        return result;
    }

    public static int Sign(bool b) {
        return b ? 1 : -1;
    }

    public static string GetHierarchicalName (this GameObject go) {
		string name = go.name;
		while (go.transform.parent != null) {

			go = go.transform.parent.gameObject;
			name = go.name + "/" + name;
		}
		return name;
	}

    public static void DrawBox(Bounds bounds, Color color) {
        Vector2 corner1;
        corner1.x = bounds.min.x;
        corner1.y = bounds.max.y;
        Vector2 corner2 = corner1;
        corner2.x = bounds.max.x;
        Debug.DrawLine(corner1, corner2, color);

        corner1 = corner2;
        corner2.y = bounds.min.y;
        Debug.DrawLine(corner1, corner2, color);

        corner1 = corner2;
        corner2.x = bounds.min.x;
        corner2.y = bounds.min.y;
        Debug.DrawLine(corner1, corner2, color);

        corner1 = corner2;
        corner2.y = bounds.max.y;
        Debug.DrawLine(corner1, corner2, color);
    }

    public static List<T> FindInterface<T>(bool includeInactive=false) {
		List<T> interfaces = new List<T>();
		GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
		foreach( var rootGameObject in rootGameObjects )
		{
			T[] childrenInterfaces = rootGameObject.GetComponentsInChildren<T>(includeInactive: includeInactive);
			foreach( var childInterface in childrenInterfaces )
			{
				interfaces.Add(childInterface);
			}
		}
		return interfaces;
	}

    public static Vector2 BottomLeftCorner(this Collider2D col) {
        return new Vector2(
			col.bounds.center.x - (col.bounds.extents.x * 0.95f * (col.transform.localScale.x > 0 ? 1 : -1)),
			col.bounds.min.y
		);
    }

    public static Vector2 BottomRightCorner(this Collider2D col) {
        return new Vector2(
			col.bounds.center.x + (col.bounds.extents.x * 0.95f * (col.transform.localScale.x > 0 ? 1 : -1)),
			col.bounds.min.y
		);
    }

    public static Vector2 Rotate(this Vector2 v, float degrees) {
        return Quaternion.Euler(0, 0, degrees) * v;
	}
}
