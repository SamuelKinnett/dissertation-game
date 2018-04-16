using System;

namespace Assets.Environment.Helpers.Pathfinding
{
    public static class NodeExtensions
    {
        public static float GetDistanceTo(this Node parentNode, Node childNode)
        {
            return (float)Math.Sqrt(Math.Pow(parentNode.X - childNode.X, 2) + Math.Pow(parentNode.Y - childNode.Y, 2));
        }
    }
}
