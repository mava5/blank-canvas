
using UnityEngine;


namespace ObjectDrawing
{
    internal class AtlasNode
    {
        public bool IsOccupied => DrawableObject != null;
        public bool IsSplit => Children != null;
        public bool IsMinSize => TextureSize == minTextureSize;
        public bool IsRoot => Parent == null;

        public int TextureSize { get; private set; } = 0;
        public int TextureIndex { get; private set; } = -1;
        public Vector2Int Min { get; private set; } = Vector2Int.zero;
        public Vector2Int Max { get; private set; } = Vector2Int.zero;
        public DrawableObject DrawableObject { get; private set; } = null;

        public int PathIndex { get; private set; } = 0;
        public AtlasNode Root { get; private set; } = null;
        public AtlasNode Parent { get; private set; } = null;
        public AtlasNode[] Children { get; private set; } = null;


        private readonly int maxTextureSize = 0;
        private readonly int minTextureSize = 0;


        #region Lifecycle =================================================================================== Lifecycle

        public AtlasNode(int textureSize, int maxTextureSize, int minTextureSize, int pathIndex, AtlasNode parent)
        {
            TextureSize = textureSize;
            this.maxTextureSize = maxTextureSize;
            this.minTextureSize = minTextureSize;
            SetParent(pathIndex, parent);
        }

        #endregion Lifecycle


        #region Public Methods ============================================================================== Public Methods

        public void SetParent(int newPathIndex, AtlasNode newParent)
        {
            if (newPathIndex >= 0 && newPathIndex < 4 && newParent != null)
            {
                PathIndex = newPathIndex;
                Parent = newParent;

                AtlasNode iteratedParent = Parent;
                while (iteratedParent?.Parent != null)
                { iteratedParent = iteratedParent.Parent; }

                Root = iteratedParent;
            }

            UpdateRegion();
        }

        public void SetObject(DrawableObject drawableObject)
        {
            DrawableObject = drawableObject;
        }

        public bool TryClear()
        {
            if (IsSplit) { return false; }

            DrawableObject = null;
            Parent?.TryMerge();

            return true;
        }

        public bool TrySplit()
        {
            if (IsSplit || IsMinSize) { return false; }

            int shrunkSize = TextureSize / 2;
            Children = new AtlasNode[4];

            for (int i = 0; i < 4; i++)
            { Children[i] = new(shrunkSize, maxTextureSize, minTextureSize, i, this); }

            return true;
        }

        public bool TryMerge()
        {
            if (!IsSplit) { return false; }

            foreach (AtlasNode child in Children)
            { if (child.IsOccupied || child.IsSplit) { return false; } }

            Children = null;

            return true;
        }

        #endregion Public Methods


        #region Private Methods ============================================================================= Private Methods

        private void UpdateRegion()
        {
            int x = 0, y = 0;
            int multiplier = 1;
            AtlasNode iteratedNode = this;

            while (iteratedNode != null)
            {
                (int xOff, int yOff) = iteratedNode.PathIndex switch
                {
                    0 => (0, 0), // Bottom-left
                    1 => (1, 0), // Bottom-right
                    2 => (0, 1), // Top-left
                    3 => (1, 1), // Top-right
                    _ => (0, 0),
                };
                x += xOff * multiplier;
                y += yOff * multiplier;
                multiplier *= 2;
                iteratedNode = iteratedNode.Parent;
            }

            TextureIndex = x + (maxTextureSize / TextureSize) * y;
            Min = new(x * TextureSize, y * TextureSize);
            Max = new((x + 1) * TextureSize, (y + 1) * TextureSize);
        }

        #endregion Private Methods
    }
}