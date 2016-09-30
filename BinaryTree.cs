using SpurRoguelike.Core.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpurRoguelike.PlayerBot
{
    public class BinaryTree : IEnumerable
    {
        public TreeNode Root { get; private set; }
        public Dictionary<Location, int> valueToKey { get; }
        public int Count { get; private set; }

        public BinaryTree()
        {
            valueToKey = new Dictionary<Location, int>();
        }

        public void Update(Location value, int newKey)
        {
            int oldKey = valueToKey[value];
            if (oldKey > newKey)
            {
                Delete(value);
                Add(newKey, value);
            }
        }
        
        public void Delete(Location value)
        {
            Count--;
            int key = valueToKey[value];
            var currentNode = Root;
            TreeNode prevNode = null;
            while (currentNode.Value != value)
            {
                prevNode = currentNode;
                if (key < currentNode.Key)
                    currentNode = currentNode.Left;
                else
                    currentNode = currentNode.Right;
            }
            if (prevNode == null)
            {
                Root = null;
                return;
            }
            if (prevNode.Key > currentNode.Key)
            {
                prevNode.Left = currentNode.Right;
                if (currentNode.Right != null)
                {
                    var leftNode = currentNode.Right;
                    while (true)
                    {
                        if (leftNode.Left == null)
                            break;
                        leftNode = leftNode.Left;
                    }   
                    leftNode.Left = currentNode.Left;
                }
                else
                    prevNode.Left = currentNode.Left;
            }
            else
            {
                prevNode.Right = currentNode.Left;
                if (currentNode.Left != null)
                {
                    TreeNode rightNode = currentNode;
                    while (true)
                    {
                        if (rightNode.Right == null)
                            break;
                        rightNode = rightNode.Right;
                    }
                    rightNode.Right = currentNode.Right;
                }
                else
                    prevNode.Right = currentNode.Right;
            }
        }

        public void Add(int key, Location value)
        {
            Count++;
            valueToKey[value] = key;
            if (Root == null)
            {
                Root = new TreeNode { Key = key, Value = value };
                return;
            }
            var currentNode = Root;
            while (true)
            {
                if (key < currentNode.Key)
                {
                    if (currentNode.Left == null)
                    {
                        currentNode.Left = new TreeNode { Key = key, Value = value };
                        break;
                    }
                    currentNode = currentNode.Left;
                }
                else
                {
                    if (currentNode.Right == null)
                    {
                        currentNode.Right = new TreeNode { Key = key, Value = value };
                        break;
                    }
                    currentNode = currentNode.Right;
                }
            }
        }

        public TreeNode ExtractMin()
        {
            Count--;
            if (Root == null) return null;
            var currentNode = Root;
            TreeNode prevNode = null;
            TreeNode prePreNode = null;
            while (currentNode != null)
            {
                prePreNode = prevNode;
                prevNode = currentNode;
                currentNode = currentNode.Left;
            }
            if (prePreNode == null)
                Root = prevNode.Right;
            else
                prePreNode.Left = prevNode.Right;
            return prevNode;
        }

        public TreeNode GetMinInSubTree(TreeNode subTreeRoot)
        {
            var currentNode = subTreeRoot;
            while (currentNode.Left != null)
                currentNode = currentNode.Left;
            return currentNode;
        }

        public bool Contains(int key)
        {
            if (Root == null)
                return false;
            var currentNode = Root;
            while (currentNode.Key.CompareTo(key) != 0)
            {
                if (currentNode.Key.CompareTo(key) > 0)
                    currentNode = currentNode.Left;
                else
                    currentNode = currentNode.Right;
                if (currentNode == null)
                    return false;
            }
            return true;
        }

        public IEnumerator<TreeNode> GetEnumerator()
        {
            var nodesToOpen = new Stack<TreeNode>();
            var currentNode = Root;
            while (true)
            {
                if (currentNode == null)
                {
                    while (currentNode == null || currentNode.Right == null)
                    {
                        if (nodesToOpen.Count == 0)
                            yield break;
                        currentNode = nodesToOpen.Pop();
                        yield return currentNode;
                        if (nodesToOpen.Count == 0 && currentNode.Left == null && currentNode.Right == null)
                            yield break;
                    }
                    currentNode = currentNode.Right;
                }
                else
                {
                    nodesToOpen.Push(currentNode);
                    currentNode = currentNode.Left;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TreeNode
    {
        public int Key { get; set; }
        public Location Value { get; set; }
        public TreeNode Left { get; set; }
        public TreeNode Right { get; set; }
    }
}