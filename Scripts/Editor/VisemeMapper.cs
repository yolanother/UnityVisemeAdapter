using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DoubTech.VisemeAdapter.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DoubTech.Lipsync
{
    public class VisemeMapper : EditorWindow
    {
        [SerializeField] private GameObject workingModel;
        private List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
        private string[] skinnedMeshRendererNames = new string[0];
        private List<List<Blendshape>> blendShapes = new List<List<Blendshape>>();
        private int blendShapeIndex;
        private int skinnedMeshRendererIndex;
        private Vector2 blendSelectionBoxSroll;
        private VisemeConfig visemeConfig;
        private ReorderableList visemeList;
        private bool _linkSides;
        
        private Dictionary<Blendshape, BlendshapeState> activeBlendshapes = new Dictionary<Blendshape, BlendshapeState>();
        private SortedList<string, BlendshapeState> activeLeftBlendshapes = new SortedList<string, BlendshapeState>();
        private SortedList<string, BlendshapeState> activeRightBlendshapes = new SortedList<string, BlendshapeState>();
        private SortedList<string, BlendshapeState> activeCenterBlendshapes = new SortedList<string, BlendshapeState>();
        private string newVisemeName;
        private VisemeMappingData activeViseme;
        private bool showAllBlendshapes;
        private int splitMode = 0;

        private class BlendshapeState
        {
            public bool selected;
            public Blendshape blendshape;
            public SkinnedMeshRenderer renderer;

            public float Value
            {
                get => renderer.GetBlendShapeWeight(blendshape.index);
                set => renderer.SetBlendShapeWeight(blendshape.index, value);
            }
        }

        [MenuItem("Tools/DoubTech/Lipsync/Viseme Mapper")]
        private static void ShowWindow()
        {
            var window = GetWindow<VisemeMapper>();
            window.titleContent = new GUIContent("Viseme Mapper");
            window.Show();
        }

        private void OnEnable()
        {
            if(workingModel) UpdateSkinnedMeshes();
            if(visemeConfig) SetVisemeConfig(visemeConfig);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is VisemeConfig c) SetVisemeConfig(c);
            Repaint();
        }

        private void UpdateSkinnedMeshes()
        {
            if (!workingModel)
            {
                skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
                this.blendShapes = new List<List<Blendshape>>();
                this.skinnedMeshRendererNames = new string[0];
                return;
            }
            
            var allSMRs = workingModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            List<string> skinnedMeshRendererNames = new List<string>();
            foreach (var skinnedMeshRenderer in allSMRs)
            {
                var mesh = skinnedMeshRenderer.sharedMesh;
                var blendShapeCounts = mesh.blendShapeCount;
                if (blendShapeCounts > 0)
                {
                    skinnedMeshRenderers.Add(skinnedMeshRenderer);
                    skinnedMeshRendererNames.Add(skinnedMeshRenderer.name);
                    List<Blendshape> blendShapeIndexes = new List<Blendshape>();
                    blendShapes.Add(blendShapeIndexes);
                    string commonName = null;
                    List<BlendshapeState> states = new List<BlendshapeState>();
                    for (int i = 0; i < blendShapeCounts; i++)
                    {
                        var blendShapeName = mesh.GetBlendShapeName(i);
                        
                        var blendShape = new Blendshape()
                        {
                            name = blendShapeName,
                            index = i,
                            parentSkinnedMeshRenderer = skinnedMeshRenderer.name
                        };
                        blendShapeIndexes.Add(blendShape);
                        var state = new BlendshapeState()
                        {
                            blendshape = blendShape,
                            renderer = skinnedMeshRenderer,
                            selected = false
                        };
                        activeBlendshapes[blendShape] = state;

                        if (visemeConfig.HasReference(blendShape))
                        {
                            if (null == commonName) commonName = blendShapeCounts > 0 ? mesh.GetBlendShapeName(0) : "";
                            for (int len = commonName.Length; len > 0; len--)
                            {
                                if (commonName == blendShape.name.Substring(0,
                                        Mathf.Min(commonName.Length, blendShape.name.Length))) break;

                                commonName = commonName.Substring(0, len - 1);
                            }

                            states.Add(state);
                        }
                    }

                    for (int i = 0; i < states.Count; i++)
                    {
                        var state = states[i];
                        var blendShapeName = state.blendshape.name;
                        if (blendShapeName.ToLower().Contains("left"))
                        {
                            var shortName = blendShapeName.Replace("left", "", StringComparison.OrdinalIgnoreCase)
                                .Substring(commonName.Length);
                            activeLeftBlendshapes[shortName] = state;
                        } else if (blendShapeName.ToLower().Contains("right"))
                        {
                            var shortName = blendShapeName.Replace("right", "", StringComparison.OrdinalIgnoreCase)
                                .Substring(commonName.Length);
                            activeRightBlendshapes[shortName] = state;
                        }
                        else
                        {
                            var shortName = blendShapeName.Substring(commonName.Length);
                            activeCenterBlendshapes[shortName] = state;
                        }
                    }
                }
            }
            
            this.skinnedMeshRendererNames = skinnedMeshRendererNames.ToArray();
        }

        private void OnGUI()
        {
            var model = (GameObject) EditorGUILayout.ObjectField("Working Model", workingModel, typeof(GameObject));
            if (model != workingModel)
            {
                workingModel = model;
                UpdateSkinnedMeshes();
            }
            var vc = (VisemeConfig) EditorGUILayout.ObjectField("Viseme Config", visemeConfig, typeof(VisemeConfig));
            if (vc != visemeConfig)
            {
                SetVisemeConfig(vc);
            }

            if (!visemeConfig)
            {
                GUILayout.Box("No Viseme Config Selected");
                return;
            }

            if (workingModel)
            {
                GUILayout.BeginHorizontal();
                skinnedMeshRendererIndex = EditorGUILayout.Popup(skinnedMeshRendererIndex, skinnedMeshRendererNames);
                showAllBlendshapes = GUILayout.Toggle(showAllBlendshapes, new GUIContent("", "Show All"), GUILayout.Width(20));
                GUILayout.EndHorizontal();
                if (skinnedMeshRendererIndex < blendShapes.Count)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("All")) splitMode = 0;
                    if (GUILayout.Button("Split")) splitMode = 1;
                    if (splitMode == 1)
                    {
                        _linkSides = GUILayout.Toggle(_linkSides, "Link Left and Right");
                    }
                    GUILayout.EndHorizontal();

                    if (splitMode == 0)
                    {
                        blendSelectionBoxSroll =
                            GUILayout.BeginScrollView(blendSelectionBoxSroll, EditorStyles.helpBox);
                        foreach (var blendShape in blendShapes[skinnedMeshRendererIndex])
                        {
                            if (activeBlendshapes.TryGetValue(blendShape, out var blendshape) &&
                                (visemeConfig.HasReference(blendshape.blendshape) || showAllBlendshapes))
                            {
                                DrawBlendshape(blendshape.blendshape.name, blendshape);
                            }
                        }

                        GUILayout.EndScrollView();
                    }
                    else
                    {
                        blendSelectionBoxSroll =
                            GUILayout.BeginScrollView(blendSelectionBoxSroll, EditorStyles.helpBox);
                        GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical();
                        foreach (var kvp in activeLeftBlendshapes)
                        {
                            DrawBlendshape(kvp.Key, kvp.Value);
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                        foreach (var kvp in activeCenterBlendshapes)
                        {
                            DrawBlendshape(kvp.Key, kvp.Value);
                        }
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                        foreach (var kvp in activeRightBlendshapes)
                        {
                            DrawBlendshape(kvp.Key, kvp.Value);
                        }
                        GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                        GUILayout.EndScrollView();
                    }
                }
            }

            if (GUILayout.Button("Clear Visemee"))
            {
                foreach (var blendshapeState in activeBlendshapes)
                {
                    blendshapeState.Value.Value = 0;
                }
            }
            GUILayout.BeginHorizontal();
            newVisemeName = EditorGUILayout.TextField("New Viseme Name", newVisemeName);
            if (GUILayout.Button("Create Viseme", GUILayout.Width(100)))
            {
                visemeConfig.visemeMappings.Add(ApplyCurrentState(new VisemeMappingData()
                {
                    name = newVisemeName
                }));
            }
            GUILayout.EndHorizontal();
            
            visemeList?.DoLayoutList();
        }

        private void DrawBlendshape(string name, BlendshapeState blendshape)
        {
            EditorGUILayout.BeginHorizontal();
            if (showAllBlendshapes)
            {
                var selected = EditorGUILayout.ToggleLeft(name,
                    visemeConfig.HasReference(blendshape.blendshape));
                if (blendshape.selected != selected)
                {
                    blendshape.selected = selected;
                    if (selected) visemeConfig.AddReference(blendshape.blendshape);
                    if (!selected) visemeConfig.RemoveReference(blendshape.blendshape);
                    EditorUtility.SetDirty(visemeConfig);
                }
            }
            else
            {
                GUILayout.Label(name);
            }

            var lastV = blendshape.Value;
            var v = GUILayout.HorizontalSlider(lastV, 0, visemeConfig.visemeMaxValue,
                GUILayout.Width(100));
            if (Math.Abs(v - lastV) > 0.0001f)
            {
                if (_linkSides)
                {
                    var isLeft = blendshape.blendshape.name.Contains("left", StringComparison.OrdinalIgnoreCase);
                    var isRight = blendshape.blendshape.name.Contains("right", StringComparison.OrdinalIgnoreCase);

                    if (isLeft || isRight)
                    {
                        if (activeLeftBlendshapes.TryGetValue(name, out var left))
                        {
                            left.Value = v;
                        }
                        if (activeRightBlendshapes.TryGetValue(name, out var right))
                        {
                            right.Value = v;
                        }
                    }
                }
                blendshape.Value = v;
            }
            EditorGUILayout.EndHorizontal();
        }

        private VisemeMappingData ApplyCurrentState(VisemeMappingData visemeMappingData)
        {
            visemeMappingData.blendshapeValues = activeBlendshapes.Where(kvp => visemeConfig.HasReference(kvp.Key)).Select(kvp =>
                new BlendshapeValue()
                {
                    blendshape = kvp.Key,
                    value = kvp.Value.Value
                }).ToArray();
            EditorUtility.SetDirty(visemeConfig);
            return visemeMappingData;
        }

        private void SetVisemeConfig(VisemeConfig vc)
        {
            visemeConfig = vc;
            visemeList = new ReorderableList(visemeConfig.visemeMappings, typeof(VisemeMappingData), true, true, true, true);
            visemeList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Visemes");
            visemeList.drawElementCallback = (rect, index, active, focused) =>
            {
                var viseme = visemeConfig.visemeMappings[index];
                // Draw the label and an edit button
                EditorGUI.LabelField(rect, viseme.name);
                if (GUI.Button(new Rect(rect.x + rect.width - 40, rect.y, 20, rect.height), EditorGUIUtility.IconContent("d_editicon.sml"), EditorStyles.iconButton))
                {
                    ApplyCurrentState(viseme);
                }
                // Add Play Button
                if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height), EditorGUIUtility.IconContent("d_PlayButton"), EditorStyles.iconButton))
                {
                    foreach (var blendshapeValue in viseme.blendshapeValues)
                    {
                        if (activeBlendshapes.TryGetValue(blendshapeValue.blendshape, out var blendshapeState))
                        {
                            blendshapeState.Value = blendshapeValue.value;
                        }
                    }
                }
            };
        }
    }
}