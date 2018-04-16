using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Assets.Environment.Helpers.Pathfinding
{
    public class Graph
    {
        private List<Node> Nodes;
        private List<Edge> Edges;

        public Graph()
        {
            Nodes = new List<Node>();
            Edges = new List<Edge>();
        }

        public void AddNode(int x, int y)
        {
            Nodes.Add(new Node(x, y));
        }

        public void RemoveNode(Node node)
        {
            var edgesToRemove = Edges.Where((e) => e.ParentNode == node || e.ChildNode == node);
            Edges = Edges.Except(edgesToRemove).ToList();
            Nodes.Remove(node);
        }

        public void AddEdge(Node parentNode, Node childNode)
        {
            Edges.Add(new Edge(parentNode, childNode));
        }

        public bool AddEdge(Vector2 parentNodePosition, Vector2 childNodePosition, bool shouldBeUnique)
        {
            var parentNode = Nodes.Single(node => node.X == parentNodePosition.x && node.Y == parentNodePosition.y);
            var childNode = Nodes.Single(node => node.X == childNodePosition.x && node.Y == childNodePosition.y);

            if (parentNode != null && childNode != null)
            {
                if (shouldBeUnique)
                {
                    // Only add this edge if it doesn't already exist
                    if (!Edges.Any(edge => (edge.ParentNode == parentNode && edge.ChildNode == childNode) || (edge.ParentNode == childNode && edge.ParentNode == parentNode)))
                    {
                        AddEdge(parentNode, childNode);
                    }
                }
                else
                {
                    AddEdge(parentNode, childNode);
                }

                return true;
            }

            return false;
        }

        public void RemoveEdge(Edge edge)
        {
            Edges.Remove(edge);
        }

        public List<Node> GetChildren(Node parentNode)
        {
            var returnList = Edges
                .Where((e) => e.ParentNode == parentNode)
                .Select((e) => e.ChildNode);

            return returnList.ToList();
        }

        public List<Edge> GetEdges(Node parentNode)
        {
            var returnList = Edges.Where((e) => e.ParentNode == parentNode || (e.ChildNode == parentNode));

            return returnList.ToList();
        }

        public float GetHeuristic(Node currentNode, Node goalNode)
        {
            return currentNode.GetDistanceTo(goalNode);
        }

        public List<Edge> FindPath(Node startNode, Node goalNode)
        {
            var OpenList = new List<Edge>();
            var ClosedList = new List<Edge>();
            var returnPath = new List<Edge>();

            var currentNode = startNode;

            while (currentNode != goalNode)
            {
                // Find the edges for the open nodes and cast them to be one directional, parent to child,
                // to make later operations easier.
                var openEdges = Edges
                    .Where((e) => e.ParentNode == currentNode || (e.ChildNode == currentNode))
                    .Select((e) => new Edge(
                        currentNode,
                        e.ParentNode == currentNode ? e.ChildNode : e.ParentNode))
                    .ToList();

                foreach (var newEdge in openEdges)
                {
                    var existingEdge = OpenList.SingleOrDefault((ol) => ol.ChildNode == newEdge.ChildNode);
                    if (existingEdge != null)
                    {
                        if (existingEdge.Cost > newEdge.Cost)
                        {
                            OpenList.Remove(existingEdge);
                            OpenList.Add(newEdge);
                        }
                    }
                    else if (!ClosedList.Any((cls) => cls.ChildNode == newEdge.ChildNode))
                    {
                        OpenList.Add(newEdge);
                    }
                }

                // Sort the open list according to weighting
                OpenList = OpenList.OrderBy((e) => e.Cost).ToList();

                // Choose the one with the lowest weighting to open
                var edgeToConsider = OpenList.First();

                OpenList.Remove(edgeToConsider);
                ClosedList.Add(edgeToConsider);

                currentNode = edgeToConsider.ChildNode;
            }

            currentNode = goalNode;

            while (currentNode != startNode)
            {
                var edgeToAdd = ClosedList.Single((e) => e.ChildNode == currentNode);
                returnPath.Add(edgeToAdd);
                currentNode = edgeToAdd.ParentNode;
            }

            returnPath.Reverse();   // Reverse it, since it was populated backwards
            return returnPath;
        }

        public bool GetDistance(Vector2 startNodePosition, Vector2 endNodePosition, out float distance)
        {
            distance = 0;

            var startNode = Nodes.Single(node => node.X == startNodePosition.x && node.Y == startNodePosition.y);
            var endNode = Nodes.Single(node => node.X == endNodePosition.x && node.Y == endNodePosition.y);

            if (startNode != null && endNode != null)
            {
                var path = FindPath(startNode, endNode);
                distance = path.Count;
                return true;
            }

            return false;
        }

        public Node GetClosestNodeToPoint(Vector2 point)
        {
            var pointAsNode = new Node((int)point.x, (int)point.y);
            Node closestNode = null;
            float closestNodeDistance = 0;

            foreach (var currentNode in Nodes)
            {
                var currentNodeDistance = currentNode.GetDistanceTo(pointAsNode);
                if (closestNode == null || currentNodeDistance < closestNodeDistance)
                {
                    closestNode = currentNode as Node;
                    closestNodeDistance = currentNodeDistance;
                }
            }

            return closestNode;
        }

        // Create and return a node without adding it to the graph
        public Node CreateTemporaryNode(Vector2 point)
        {
            return new Node((int)point.x, (int)point.y);
        }
    }
}

