using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DoubTech.VisemeAdapter.Data
{
    [CreateAssetMenu(fileName = "VisemeConfig", menuName = "DoubTech/Lipsync/Viseme Config", order = 0)]
    public class VisemeConfig : ScriptableObject
    {
        [SerializeField] internal GameObject baseCharacterPrefab;
        [SerializeField] public List<VisemeMappingData> visemeMappings = new List<VisemeMappingData>();
        [SerializeField] public List<Blendshape> blendshapeReferences = new List<Blendshape>();

        private Dictionary<string, Blendshape> _blendshapeReferenceMap;
        public Dictionary<string, Blendshape> BlendshapeReferenceMap
        {
            get
            {
                if (null == _blendshapeReferenceMap)
                {
                    _blendshapeReferenceMap = new Dictionary<string, Blendshape>();
                    foreach (var reference in blendshapeReferences)
                    {
                        _blendshapeReferenceMap[reference.ToString()] = reference;
                    }
                }

                return _blendshapeReferenceMap;
            }
        }

        private Dictionary<string, VisemeMappingData> visemeBlendshapes;
        public Dictionary<string, VisemeMappingData> VisemeBlendshapes
        {
            get
            {
                if (null == visemeBlendshapes)
                {
                    visemeBlendshapes = new Dictionary<string, VisemeMappingData>();
                    foreach (var mapping in visemeMappings)
                    {
                        visemeBlendshapes[mapping.name] = mapping;
                    }
                }

                return visemeBlendshapes;
            }
        }

        public void AddReference(Blendshape blendshape)
        {
            if (!BlendshapeReferenceMap.ContainsKey(blendshape.ToString()))
            {
                blendshapeReferences.Add(blendshape);
                _blendshapeReferenceMap[blendshape.ToString()] = blendshape;
            }
        }

        public void RemoveReference(Blendshape blendshape)
        {
            if (BlendshapeReferenceMap.ContainsKey(blendshape.ToString()))
            {
                _blendshapeReferenceMap.Remove(blendshape.ToString());
                blendshapeReferences.Remove(blendshape);
            }
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }

        public bool HasReference(Blendshape blendshape) => BlendshapeReferenceMap.ContainsKey(blendshape.ToString());
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VisemeConfig))]
    public class VisemeConfigEditor : Editor
    {
        private VisemeConfig visemeConfig;
        private bool initialized;
        private SkinnedMeshRenderer[] skinnedMeshRenderers;

        private void OnValidate()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            initialized = true;
            visemeConfig = (VisemeConfig)target;
            if (visemeConfig.baseCharacterPrefab)
            {
                skinnedMeshRenderers = visemeConfig.baseCharacterPrefab.GetComponents<SkinnedMeshRenderer>();
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    var mesh = skinnedMeshRenderer.sharedMesh;
                    var blendShapes = mesh.blendShapeCount;
                    for (int i = 0; i < blendShapes; i++)
                    {
                        var blendShapeName = mesh.GetBlendShapeName(i);
                        Debug.Log(blendShapeName);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if(!initialized) Initialize();
            base.OnInspectorGUI();

            GUILayout.Label("Skinned Mesh Renderers", EditorStyles.boldLabel);
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                GUILayout.Label(skinnedMeshRenderer.name);
            }
        }
    }
    #endif
}