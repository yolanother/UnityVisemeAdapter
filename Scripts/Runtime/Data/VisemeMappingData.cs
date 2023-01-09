using System;

namespace DoubTech.VisemeAdapter.Data
{
    [Serializable]
    public class VisemeMappingData
    {
        public string name;
        public BlendshapeValue[] blendshapeValues;

        public override string ToString()
        {
            return name;
        }
    }
}