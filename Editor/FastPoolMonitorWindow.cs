using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vtcong.Pool.Editor
{
    public sealed class FastPoolMonitorWindow : EditorWindow
    {
        private readonly List<FastPoolInfo> _infos = new();
        private Vector2 _scroll;

        [MenuItem("Window/VTC/Fast Pool Monitor")]
        private static void Open()
        {
            GetWindow<FastPoolMonitorWindow>("Fast Pool");
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Chi hien thi khi dang Play.", MessageType.Info);
                return;
            }

            VtcFastPool.GetPoolInfos(_infos);

            int totalIdle = 0;
            int totalActive = 0;
            for (int i = 0; i < _infos.Count; i++)
            {
                totalIdle += _infos[i].IdleCount;
                totalActive += _infos[i].ActiveCount;
            }

            EditorGUILayout.LabelField($"Pools: {_infos.Count}   Idle: {totalIdle}   Active: {totalActive}",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ID", GUILayout.Width(30));
                GUILayout.Label("Prefab", GUILayout.MinWidth(120));
                GUILayout.Label("Idle", GUILayout.Width(45));
                GUILayout.Label("Active", GUILayout.Width(45));
                GUILayout.Label("Cap", GUILayout.Width(45));
                GUILayout.Label("Keep", GUILayout.Width(35));
                GUILayout.Space(50);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _infos.Count; i++)
            {
                FastPoolInfo info = _infos[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(info.Id.ToString(), GUILayout.Width(30));

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(info.Prefab, typeof(GameObject), false, GUILayout.MinWidth(120));
                    }

                    GUILayout.Label(info.IdleCount.ToString(), GUILayout.Width(45));
                    GUILayout.Label(info.ActiveCount.ToString(), GUILayout.Width(45));
                    GUILayout.Label(info.Capacity > 0 ? info.Capacity.ToString() : "-", GUILayout.Width(45));
                    GUILayout.Label(info.KeepParentOnReturn ? "x" : "", GUILayout.Width(35));

                    if (GUILayout.Button("Clear", GUILayout.Width(50)) && info.Prefab != null)
                        VtcFastPool.ClearPool(info.Prefab);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
