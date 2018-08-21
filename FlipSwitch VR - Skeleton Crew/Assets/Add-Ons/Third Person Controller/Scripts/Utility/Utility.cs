using UnityEngine;
using System;
using System.Collections.Generic;

namespace Opsive.ThirdPersonController
{
    /// <summary>
    /// A collection of small utility methods.
    /// </summary>
    public static class Utility
    {
        // Internal variables
        private static Dictionary<string, Type> s_TypeLookup = new Dictionary<string, Type>();
        private static List<string> s_LoadedAssemblies = null;
        private static Dictionary<GameObject, Camera> s_GameObjectCameraMap = new Dictionary<GameObject, Camera>();
        private static Dictionary<GameObject, Dictionary<Type, Component>> s_GameObjectComponentMap = new Dictionary<GameObject, Dictionary<Type, Component>>();

        /// <summary>
        /// Returns a cached component reference for the specified type.
        /// </summary>
        /// <param name="target">The GameObject (or child GameObject) to get the component reference of.</param>
        /// <param name="type">The type of component to get.</param>
        /// <returns>The cached component reference.</returns>
        public static T GetComponentForType<T>(GameObject target) where T : Component
        {
            return GetComponentForType<T>(target, false);
        }

        /// <summary>
        /// Returns a cached component reference for the specified type.
        /// </summary>
        /// <param name="target">The GameObject (or child GameObject) to get the component reference of.</param>
        /// <param name="type">The type of component to get.</param>
        /// <param name="allowParentComponents">Should the component be retrieved in a parent component?</param>
        /// <returns>The cached component reference.</returns>
        public static T GetComponentForType<T>(GameObject target, bool allowParentComponents) where T : Component
        {
            Dictionary<Type, Component> typeComponentMap;
            Component targetComponent;
            if (s_GameObjectComponentMap.TryGetValue(target, out typeComponentMap)) {
                if (typeComponentMap.TryGetValue(typeof(T), out targetComponent)) {
                    return targetComponent as T;
                }
            } else {
                typeComponentMap = new Dictionary<Type, Component>();
                s_GameObjectComponentMap.Add(target, typeComponentMap);
            }

            if (allowParentComponents) {
                targetComponent = target.GetComponentInParent(typeof(T));
            } else {
                targetComponent = target.GetComponent(typeof(T));
            }
            typeComponentMap.Add(typeof(T), targetComponent);
            return targetComponent as T;
        }

        /// <summary>
        /// Searches through all of the loaded assembies for the specified type.
        /// </summary>
        /// <param name="name">The string value of the type.</param>
        /// <returns>The found Type. Can be null.</returns>
        public static Type GetType(string name)
        {
            Type type;
            // Cache the results for quick repeated lookup.
            if (s_TypeLookup.TryGetValue(name, out type)) {
                return type;
            }

            type = Type.GetType(name);
            // Look in the loaded assemblies.
            if (type == null) {
                if (s_LoadedAssemblies == null) {
#if NETFX_CORE && !UNITY_EDITOR
                    s_LoadedAssemblies = GetStorageFileAssemblies(typeName).Result;
#else
                    s_LoadedAssemblies = new List<string>();
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < assemblies.Length; ++i) {
                        s_LoadedAssemblies.Add(assemblies[i].FullName);
                    }
#endif
                }
                // Continue until the type is found.
                for (int i = 0; i < s_LoadedAssemblies.Count; ++i) {
                    type = Type.GetType(name + "," + s_LoadedAssemblies[i]);
                    if (type != null) {
                        break;
                    }
                }
            }
            if (type != null) {
                s_TypeLookup.Add(name, type);
            }
            return type;
        }

        /// <summary>
        /// Restricts the angle between -360 and 360 degrees.
        /// </summary>
        /// <param name="angle">The angle to restrict.</param>
        /// <returns>An angle between -360 and 360 degrees.</returns>
        public static float RestrictAngle(float angle)
        {
            if (angle < -360) {
                angle += 360;
            }
            if (angle > 360) {
                angle -= 360;
            }
            return angle;
        }

        /// <summary>
        /// Restricts the angle between -180 and 180 degrees.
        /// </summary>
        /// <param name="angle">The angle to restrict.</param>
        /// <returns>An angle between -180 and 180 degrees.</returns>
        public static float RestrictInnerAngle(float angle)
        {
            if (angle < -180) {
                angle += 360;
            }
            if (angle > 180) {
                angle -= 360;
            }
            return angle;
        }

        /// <summary>
        /// Restricts the angle between the firstAmount and secondAmount.
        /// </summary>
        /// <param name="angle">The angle to restrict.</param>
        /// <param name="firstAmount">The first amount to restrict the angle by.</param>
        /// <param name="secondAmount">The second amount to restrict the angle by.</param>
        /// <returns></returns>
        public static float RestrictAngleBetween(float currentAngle, float angle, float firstAmount, float secondAmount)
        {
            var lowerAngle = Utility.RestrictInnerAngle(currentAngle + firstAmount);
            var upperAngle = Utility.RestrictInnerAngle(currentAngle + secondAmount);
            if (upperAngle < lowerAngle) {
                upperAngle += 360;
            }
            // Keep the angle in the same restricted angle to ease the smoothing.
            if (angle < upperAngle - 360) {
                angle += 360;
            } else if (angle > lowerAngle + 360) {
                angle -= 360;
            }
            return Mathf.Clamp(angle, lowerAngle, upperAngle);
        }

        /// <summary>
        /// Clamp the angle between the min and max angle values.
        /// </summary>
        /// <param name="angle">The angle to be clamped.</param>
        /// <param name="min">The minimum angle value.</param>
        /// <param name="max">The maximum angle value.</param>
        /// <returns></returns>
        public static float ClampAngle(float angle, float min, float max)
        {
            return Mathf.Clamp(RestrictAngle(angle), min, max);
        }

        /// <summary>
        /// Returns true if layer is within the layerMask.
        /// </summary>
        /// <param name="layer">The layer to check.</param>
        /// <param name="layerMask">The mask to compare against.</param>
        /// <returns>True if the layer is within the layer mask.</returns>
        public static bool InLayerMask(int layer, int layerMask)
        {
            return ((1 << layer) & layerMask) == (1 << layer);
        }

        /// <summary>
        /// Returns the camera with the MainCamera tag or the camera with the CameraMonitor attached.
        /// </summary>
        /// <param name="character">The character that the camera is attached to.</param>
        /// <returns>The found camera (if any).</returns>
        public static Camera FindCamera(GameObject character)
        {
            Camera camera;
            if (s_GameObjectCameraMap.TryGetValue(character, out camera)) {
                // The reference may be null if the scene changed.
                if (camera != null) {
                    return camera;
                }
                // The reference is null - search for the camera again.
                s_GameObjectCameraMap.Remove(character);
            }
            // First try to find the camera with the character attached. If no camera has the character attached the return the first camera with the CameraMonitor.
            if ((camera = SearchForCamera(character)) != null || (camera = SearchForCamera(null)) != null) {
                s_GameObjectCameraMap.Add(character, camera);
                return camera;
            } else {
                s_GameObjectCameraMap.Add(character, null);
                Debug.LogWarning("No camera exists with the CameraMonitor component. Has this component been added to a camera?");
            }
            return null;
        }

        /// <summary>
        /// Loops through the cameras searching for a camera with the character assigned.
        /// </summary>
        /// <param name="character">The character to search for. Can be null.</param>
        /// <returns>The camera with the character assigned.</returns>
        private static Camera SearchForCamera(GameObject character)
        {
            CameraMonitor cameraMonitor;
            if (Camera.main != null && (cameraMonitor = Camera.main.GetComponent<CameraMonitor>()) != null && (character == null || cameraMonitor.Character == character)) {
                return Camera.main;
            }
            for (int i = 0; i < Camera.allCameras.Length; ++i) {
                if ((cameraMonitor = Camera.allCameras[i].GetComponent<CameraMonitor>()) != null && (character == null || cameraMonitor.Character == character)) {
                    return Camera.allCameras[i];
                }
            }
            return null;
        }
    }
}