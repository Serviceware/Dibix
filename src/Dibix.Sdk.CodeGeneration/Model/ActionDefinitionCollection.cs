using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionDefinitionCollection : IReadOnlyList<ActionDefinition>
    {
        #region Fields
        private readonly ICollection<ActionDefinitionKey> _keys = new HashSet<ActionDefinitionKey>();
        private readonly IList<ActionDefinition> _actions = new Collection<ActionDefinition>();
        #endregion

        #region Properties
        public ActionDefinition this[int index] => this._actions[index];
        public int Count => this._actions.Count;
        #endregion

        #region Internal Methods
        internal bool TryAdd(ActionDefinition actionDefinition)
        {
            if (this._keys.Contains(new ActionDefinitionKey(actionDefinition)))
                return false;

            this._keys.Add(new ActionDefinitionKey(actionDefinition));
            this._actions.Add(actionDefinition);
            return true;
        }
        #endregion

        #region Public Methods
        IEnumerator<ActionDefinition> IEnumerable<ActionDefinition>.GetEnumerator() => this._actions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this._actions.GetEnumerator();
        #endregion

        #region Nested Types
        private readonly struct ActionDefinitionKey
        {
            public ActionMethod Method { get; }
            public string ChildRoute { get; }

            public ActionDefinitionKey(ActionDefinition item)
            {
                this.Method = item.Method;
                this.ChildRoute = item.ChildRoute;
            }
        }
        #endregion
    }
}