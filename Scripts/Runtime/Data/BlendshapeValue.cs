using System;

namespace DoubTech.VisemeAdapter.Data
{
    [Serializable]
    public class BlendshapeValue
    {
        public Blendshape blendshape;
        public float value;

        public string name => blendshape?.name;
        public override string ToString() => blendshape?.name;
    }
}