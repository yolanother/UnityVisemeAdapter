using System;
using System.Collections.Generic;

namespace DoubTech.VisemeAdapter.Data
{

    [Serializable]
    public class Blendshape
    {
        private sealed class BlendshapeRelationalComparer : IComparer<Blendshape>
        {
            public int Compare(Blendshape x, Blendshape y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                var nameComparison = string.Compare(x.name, y.name, StringComparison.Ordinal);
                if (nameComparison != 0) return nameComparison;
                var parentSkinnedMeshRendererComparison = string.Compare(x.parentSkinnedMeshRenderer, y.parentSkinnedMeshRenderer, StringComparison.Ordinal);
                if (parentSkinnedMeshRendererComparison != 0) return parentSkinnedMeshRendererComparison;
                return x.index.CompareTo(y.index);
            }
        }

        public static IComparer<Blendshape> BlendshapeComparer { get; } = new BlendshapeRelationalComparer();

        public string name;
        public string parentSkinnedMeshRenderer;
        public int index;

        public override string ToString()
        {
            return parentSkinnedMeshRenderer + "::" + name;
        }
        protected bool Equals(Blendshape other)
        {
            return parentSkinnedMeshRenderer == other.parentSkinnedMeshRenderer && index == other.index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Blendshape)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(parentSkinnedMeshRenderer, index);
        }
    }

}