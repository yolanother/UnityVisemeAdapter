using System;
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
        
        private Dictionary<Blendshape, BlendshapeState> activeBlendshapes = new Dictionary<Blendshape, BlendshapeState>();
        private string newVisemeName;
        private VisemeMappingData activeViseme;
        private bool showAllBlendshapes;

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
                        activeBlendshapes[blendShape] = new BlendshapeState()
                        {
                            blendshape = blendShape,
                            renderer = skinnedMeshRenderer,
                            selected = false
                        };
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
                    blendSelectionBoxSroll = GUILayout.BeginScrollView(blendSelectionBoxSroll, EditorStyles.helpBox,
                        GUILayout.MaxHeight(200));
                    foreach (var blendShape in blendShapes[skinnedMeshRendererIndex])
                    {
                        if (activeBlendshapes.TryGetValue(blendShape, out var blendshape) && (visemeConfig.HasReference(blendshape.blendshape) || showAllBlendshapes))
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (showAllBlendshapes)
                            {
                                var selected = EditorGUILayout.ToggleLeft(blendShape.name,
                                    visemeConfig.HasReference(blendshape.blendshape));
                                blendshape.selected = selected;
                                if (blendshape.selected != selected)
                                {
                                    if (selected) visemeConfig.AddReference(blendshape.blendshape);
                                    if (!selected) visemeConfig.RemoveReference(blendshape.blendshape);
                                    EditorUtility.SetDirty(visemeConfig);
                                }
                            }
                            else
                            {
                                GUILayout.Label(blendShape.name);
                            }

                            var lastV = blendshape.Value;
                            var v = GUILayout.HorizontalSlider(lastV, 0, visemeConfig.visemeMaxValue, GUILayout.Width(100));
                            if (Math.Abs(v - lastV) > 0.0001f) blendshape.Value = v;
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.EndScrollView();
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