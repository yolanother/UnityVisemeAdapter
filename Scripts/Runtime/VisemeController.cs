using System;
using System.Collections;
using System.Collections.Generic;
using DoubTech.VisemeAdapter.Data;
using UnityEngine;

namespace DoubTech.VisemeAdapter
{
    public class VisemeController : MonoBehaviour
    {
        [SerializeField] private VisemeConfig visemeConfig;
        [SerializeField] private float transitionSpeed = .25f;
        [SerializeField] private GameObject skinnedMeshRoot;
        [SerializeField] private float valueMultiplier = 1;

        private Dictionary<string, SkinnedMeshRenderer> _skinnedMeshRenderers;
        private List<KeyValuePair<SkinnedMeshRenderer,BlendshapeValue>> activeShapes;
        private float time;

        private Dictionary<string, SkinnedMeshRenderer> SkinnedMeshRenderers
        {
            get
            {
                if (null == _skinnedMeshRenderers)
                {
                    _skinnedMeshRenderers = new Dictionary<string, SkinnedMeshRenderer>();
                    var skinnedMeshRenderers = skinnedMeshRoot ?
                        skinnedMeshRoot.GetComponentsInChildren<SkinnedMeshRenderer>() :
                        GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (var meshRenderer in skinnedMeshRenderers)
                    {
                        _skinnedMeshRenderers[meshRenderer.name] = meshRenderer;
                    }

                    if (_skinnedMeshRenderers.Count == 0)
                    {
                        Debug.LogError("No skinned mesh renderers found!");
                    }
                }

                return _skinnedMeshRenderers;
            }
        }
        
        public void SetViseme(string viseme)
        {
            StopAllCoroutines();
            if (Application.isPlaying)
            {
                StartCoroutine(Transition(viseme));
            }
            else
            {
                SetVisemeImmediate(viseme);
            }
        }

        private IEnumerator Transition(string viseme)
        {
            activeShapes = new List<KeyValuePair<SkinnedMeshRenderer, BlendshapeValue>>();
            if(visemeConfig.VisemeBlendshapes.TryGetValue(viseme, out var blendshapes))
            {
                foreach (var blendshapeValue in blendshapes.blendshapeValues)
                {
                    if (SkinnedMeshRenderers.TryGetValue(blendshapeValue.blendshape.parentSkinnedMeshRenderer, out var mr))
                    {
                        activeShapes.Add(new KeyValuePair<SkinnedMeshRenderer, BlendshapeValue>(mr, blendshapeValue));
                    }
                }
            }

            time = Time.deltaTime;
            while (time < transitionSpeed)
            {
                foreach (var activeShape in activeShapes)
                {
                    var current = activeShape.Key.GetBlendShapeWeight(activeShape.Value.blendshape.index);
                    var target = activeShape.Value.value;
                    var value = Mathf.Lerp(current, target, time / transitionSpeed);
                    activeShape.Key.SetBlendShapeWeight(activeShape.Value.blendshape.index, value);
                }

                time += Time.deltaTime;
                yield return null;
            }
        }

        private void LateUpdate()
        {
            if (time < transitionSpeed && time > 0)
            {
                foreach (var activeShape in activeShapes)
                {
                    var current = activeShape.Key.GetBlendShapeWeight(activeShape.Value.blendshape.index);
                    var target = activeShape.Value.value;
                    var value = Mathf.Lerp(current, target, time / transitionSpeed);
                    activeShape.Key.SetBlendShapeWeight(activeShape.Value.blendshape.index, value * valueMultiplier);
                }
            }
        }

        private void SetVisemeImmediate(string viseme)
        {
            if(visemeConfig.VisemeBlendshapes.TryGetValue(viseme, out var blendshapes))
            {
                foreach (var blendshapeValue in blendshapes.blendshapeValues)
                {
                    if (SkinnedMeshRenderers.TryGetValue(blendshapeValue.blendshape.parentSkinnedMeshRenderer, out var mr))
                    {
                        mr.SetBlendShapeWeight(blendshapeValue.blendshape.index, blendshapeValue.value);
                    }
                }
            }
        }
    }
}