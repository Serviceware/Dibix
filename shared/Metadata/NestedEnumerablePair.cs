namespace Dibix
{
    internal sealed class NestedEnumerablePair<TParent, TChild>
    {
        public int ParentIndex { get; }
        public int ChildIndex { get; }
        public TParent Parent { get; }
        public TChild Child { get; }

        public NestedEnumerablePair(int parentIndex, int childIndex, TParent parent, TChild child)
        {
            ParentIndex = parentIndex;
            ChildIndex = childIndex;
            Parent = parent;
            Child = child;
        }
    }
}