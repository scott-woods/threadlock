using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Threadlock.Models
{
    public class DungeonGraph
    {
        HashSet<DungeonNode> _visited = new HashSet<DungeonNode>();
        HashSet<DungeonNode> _recursionStack = new HashSet<DungeonNode>();
        List<List<DungeonNode>> _loops = new List<List<DungeonNode>>();
        public List<List<DungeonNode>> Loops { get => _loops; }
        List<List<DungeonNode>> _trees = new List<List<DungeonNode>>();
        public List<List<DungeonNode>> Trees { get => _trees; }
        List<DungeonNode> _allNodes = new List<DungeonNode>();
        Dictionary<DungeonNode, DungeonNode> _parentMap = new Dictionary<DungeonNode, DungeonNode>();

        public void ProcessGraph(List<DungeonNode> nodes)
        {
            _allNodes = nodes;

            //find loops
            DFS(null, nodes.First());

            //clear visited nodes to prepare processing for trees
            _visited.Clear();

            //find trees
            foreach (var node in _allNodes)
            {
                if (!_visited.Contains(node) && !IsInAnyLoop(node))
                {
                    List<DungeonNode> tree = new List<DungeonNode>();
                    FindTree(node, tree);
                    _trees.Add(tree);
                }
            }
        }

        void DFS(DungeonNode parent, DungeonNode node)
        {
            //add node to both visited and recursion stack
            _visited.Add(node);
            _recursionStack.Add(node);

            //add to parent map
            _parentMap[node] = parent;

            //loop through node children
            foreach (var child in node.Children)
            {
                var childNode = _allNodes.Find(n => n.Id == child.ChildNodeId);

                //if the child node hasn't been visited, search it
                if (!_visited.Contains(childNode))
                    DFS(node, childNode);

                //if the child is in the stack, we've found a loop
                else if (_recursionStack.Contains(childNode))
                    FindLoop(childNode, node);
            }

            _recursionStack.Remove(node);
        }

        void FindLoop(DungeonNode startNode, DungeonNode currentNode)
        {
            List<DungeonNode> loop = new List<DungeonNode>();

            DungeonNode temp = currentNode;

            while (temp != startNode)
            {
                loop.Add(temp);
                temp = _parentMap.ContainsKey(temp) ? _parentMap[temp] : null;
                if (temp == null)
                    break;
            }

            loop.Add(startNode);

            loop.Reverse();

            _loops.Add(loop);
        }

        void FindTree(DungeonNode node, List<DungeonNode> currentTree)
        {
            _visited.Add(node);
            currentTree.Add(node);

            foreach (var child in node.Children)
            {
                var childNode = _allNodes.Find(n => n.Id == child.ChildNodeId);

                if (!_visited.Contains(childNode) && !IsInAnyLoop(childNode))
                {
                    FindTree(childNode, currentTree);
                }
            }
        }

        bool IsInAnyLoop(DungeonNode node)
        {
            return _loops.Any(l => l.Contains(node));
        }
    }
}
