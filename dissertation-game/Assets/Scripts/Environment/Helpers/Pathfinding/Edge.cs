namespace Assets.Environment.Helpers.Pathfinding
{
    public class Edge
    {
        public Node ParentNode;
        public Node ChildNode;
        public Node GoalNode;   // Only used for pathfinding

        public float Distance
        {
            get
            {
                return ParentNode.GetDistanceTo(ChildNode);
            }
        }

        public float Cost
        {
            get
            {
                return Distance + ChildNode.GetDistanceTo(GoalNode);
            }
        }

        public Edge(Node parentNode, Node childNode)
        {
            ParentNode = parentNode;
            ChildNode = childNode;
        }

        public Edge(Node parentNode, Node childNode, Node goalNode)
        {
            ParentNode = parentNode;
            ChildNode = childNode;
            GoalNode = goalNode;
        }
    }
}

