using UnityEngine;
using UnityEditor;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks.ThirdPersonController;
using Opsive.ThirdPersonController.Abilities;
using System.Collections.Generic;
using System;

namespace BehaviorDesigner.Editor.ThirdPersonController.ObjectDrawers
{
    [CustomObjectDrawer(typeof(AbilityDrawerAttribute))]
    public class AbilityDrawer : ObjectDrawer
    {
        private static List<Type> m_AbilityTypes;
        private static string[] m_AbilityNames;
        private int index = -1;

        public override void OnGUI(GUIContent label)
        {
            var abilityNameList = new List<string>();
            m_AbilityTypes = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; ++i) {
                var assemblyTypes = assemblies[i].GetTypes();
                for (int j = 0; j < assemblyTypes.Length; ++j) {
                    // Ignore the Third Person Controller wrapper ability classes.
                    if (typeof(Ability).IsAssignableFrom(assemblyTypes[j]) && 
                        (assemblyTypes[j].Namespace == null || !assemblyTypes[j].Namespace.Equals("Opsive.ThirdPersonController.Wappers.Abilities")) && 
                        !assemblyTypes[j].IsAbstract) {
                        m_AbilityTypes.Add(assemblyTypes[j]);
                        abilityNameList.Add(assemblyTypes[j].Name);
                    }
                }
            }
            m_AbilityNames = abilityNameList.ToArray();
            var abilityName = (value as SharedString).Value;
            if (index == -1) {
                index = 0;
                if (!string.IsNullOrEmpty(abilityName)) {
                    for (int i = 0; i < m_AbilityTypes.Count; ++i) {
                        if (m_AbilityTypes[i].FullName.Equals(abilityName)) {
                            index = i;
                            break;
                        }
                    }
                }
            }

            index = EditorGUILayout.Popup(label.text, index, m_AbilityNames);
            if (!m_AbilityTypes[index].Equals(abilityName)) {
                (value as SharedString).Value = m_AbilityTypes[index].FullName;
            }
        }
    }
}