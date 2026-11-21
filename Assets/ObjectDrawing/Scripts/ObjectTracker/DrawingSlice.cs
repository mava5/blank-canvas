
using System.Collections.Generic;
using UnityEngine;
using Utility;


namespace ObjectDrawing
{
    internal class DrawingSlice
    {
        public int Index { get; private set; } = 0;

        public bool IsEmpty => root == null || (!root.IsOccupied && !root.IsSplit);


        private AtlasNode root = null;

        private Dictionary<DrawableObject, AtlasNode> objectToNode = new();

        private int maxTextureSize = 0;
        private int minTextureSize = 0;


        public DrawingSlice(int sliceIndex, int maxTextureSize, int minTextureSize)
        {
            Index = sliceIndex;
            this.maxTextureSize = maxTextureSize;
            this.minTextureSize = minTextureSize;
            root = new(maxTextureSize, maxTextureSize, minTextureSize, 0, null);
        }


        public bool Contains(DrawableObject drawableObject) => objectToNode.ContainsKey(drawableObject);

        public List<DrawableObject> GetObjects()
        {
            Stack<AtlasNode> toCheck = new();
            List<DrawableObject> drawableObjects = new();

            toCheck.Push(root);

            while (toCheck.Count > 0)
            {
                AtlasNode nodeToCheck = toCheck.Pop();

                if (nodeToCheck.IsSplit)
                {
                    foreach (AtlasNode child in nodeToCheck.Children)
                    { toCheck.Push(child); }
                }
                else if (nodeToCheck.IsOccupied)
                {
                    drawableObjects.Add(nodeToCheck.DrawableObject);
                }
            }

            return drawableObjects;
        }

        public bool TryRemove(DrawableObject drawableObject)
        {
            if (!Contains(drawableObject)) { return false; }

            if (!objectToNode[drawableObject].TryClear()) { return false; }

            objectToNode.Remove(drawableObject);

            return true;
        }

        public bool TryAdd(DrawableObject drawableObject, out AtlasNode assignedNode)
        {
            assignedNode = null;

            int targetTextureSize = drawableObject.TextureSize;

            if (targetTextureSize < minTextureSize)
            { Util.LogError<DrawableObjectTracker>("Object target texture too small!"); return false; }

            Stack<AtlasNode> toSearch = new();
            toSearch.Push(root);

            while (toSearch.Count > 0)
            {
                AtlasNode node = toSearch.Pop();

                // Too small
                if (node.TextureSize < targetTextureSize) { continue; }

                // Just right
                else if (node.TextureSize == targetTextureSize)
                {
                    if (node.IsSplit || node.IsOccupied) { continue; }
                    
                    if (TrySetObjectForNode(node, drawableObject))
                    {
                        assignedNode = node;
                        return true;
                    }
                }

                // Too big
                else if (!node.IsOccupied)
                {
                    node.TrySplit();

                    // Go in reverse order so that first child is checked next
                    for (int i = 3; i >= 0; i--)
                    { toSearch.Push(node.Children[i]); }
                }
            }

            return false;
        }

        private bool TrySetObjectForNode(AtlasNode node, DrawableObject drawableObject)
        {
            if (node == null || drawableObject == null) { return false; }

            if (node.IsSplit || node.IsOccupied || drawableObject.TextureSize != node.TextureSize) { return false; }

            node.SetObject(drawableObject);
            objectToNode.TryAdd(drawableObject, node);

            return true;
        }

        public void LogContents()
        {
            int maxIndent = Mathf.FloorToInt(Mathf.Log(maxTextureSize, 2));

            Util.Log<DrawingSlice>(Index + " ===============================================");

            string toLog = "";

            Stack<AtlasNode> toProcess = new(new List<AtlasNode>() { root });
            while (toProcess.Count > 0)
            {
                AtlasNode node = toProcess.Pop();

                if (node.IsRoot)
                {
                    toLog += "Root\n";
                }
                else
                {
                    toLog += new string('=', Mathf.FloorToInt(maxIndent - Mathf.Log(node.TextureSize, 2)) * 2);
                    toLog += $" {node.PathIndex} | {node.TextureIndex}: " + (node.IsOccupied ? node.DrawableObject.name : "");
                    toLog += "\n";
                }

                if (node.IsSplit)
                {
                    for (int i = 3; i >= 0; i--)
                    { toProcess.Push(node.Children[i]); }
                }
            }

            Util.Log<DrawingSlice>(toLog);
        }
    }
}
