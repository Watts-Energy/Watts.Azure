namespace Watts.Azure.Common.General
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Originally taken from https://stackoverflow.com/questions/66893/tree-data-structure-in-c-sharp/15101910
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TreeNode<T>
    {
        private readonly List<TreeNode<T>> children = new List<TreeNode<T>>();

        public TreeNode(T value)
        {
            this.Value = value;
        }

        public TreeNode<T> this[int i] => this.children[i];

        public TreeNode<T> Parent { get; private set; }

        public T Value { get; }

        public ReadOnlyCollection<TreeNode<T>> Children => this.children.AsReadOnly();

        public TreeNode<T> AddChild(TreeNode<T> childNode)
        {
            this.children.Add(childNode);
            return childNode;
        }

        public TreeNode<T>[] AddChildren(params TreeNode<T>[] values)
        {
            return values.Select(this.AddChild).ToArray();
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            return this.children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(this.Value);
            foreach (var child in this.children)
                child.Traverse(action);
        }

        public void TraverseParallel(Action<T> action)
        {
            action(this.Value);
            Parallel.ForEach(this.children, n =>
            {
                n.Traverse(action);
            });
        }

        public bool IsLeaf => this.Children.Count == 0;

        public int Level
        {
            get
            {
                if (this.Parent == null)
                {
                    return 1;
                }

                return this.Parent.Level + 1;
            }
        }

        public int TotalNumberOfNodes => this.Flatten().Count();

        public IEnumerable<TreeNode<T>> FlattenNodes()
        {
            return new[] { this }.Union(this.children.SelectMany(x => x.FlattenNodes()));
        }

        public IEnumerable<T> Flatten()
        {
            return new[] {this.Value }.Union(this.children.SelectMany(x => x.Flatten()));
        }
    }
}
