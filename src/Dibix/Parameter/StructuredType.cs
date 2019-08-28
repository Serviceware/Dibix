using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.SqlServer.Server;

namespace Dibix
{
    public abstract class StructuredType : IEnumerable<SqlDataRecord>
    {
        #region Fields
        private readonly ICollection<SqlDataRecord> _records;
        private SqlMetaData[] _metadata;
        #endregion

        #region Properties
        public string TypeName { get; }
        #endregion

        #region Constructor
        protected StructuredType(string typeName)
        {
            this._records = new Collection<SqlDataRecord>();
            this.TypeName = typeName;
        }
        #endregion

        #region Public Methods
        public string Dump()
        {
            return SqlDataRecordDiagnostics.Dump(this._metadata, this._records);
        }
        
        public IEnumerable<SqlDataRecord> GetRecords()
        {
            return this._records;
        }
        #endregion

        #region Protected Methods
        protected void ImportSqlMetadata(Expression<Action> addMethodExpression)
        {
            this._metadata = SqlMetaDataAccessor.GetMetadata(this.GetType(), addMethodExpression);
        }

        protected internal void AddItem(params object[] values)
        {
            if (this._metadata == null)
                throw new InvalidOperationException("Please define metadata by calling ImportSqlMetadata() in your constructor");

            SqlDataRecord record = new SqlDataRecord(this._metadata);
            record.SetValues(values);
            this._records.Add(record);
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._records.GetEnumerator();
        }
        #endregion

        #region IEnumerable<SqlDataRecord> Members
        IEnumerator<SqlDataRecord> IEnumerable<SqlDataRecord>.GetEnumerator()
        {
            return this._records.GetEnumerator();
        }
        #endregion
    }
    
    public abstract class StructuredType<TDefinition> : StructuredType where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        public static TDefinition From<TSource>(IEnumerable<TSource> source, Action<TDefinition, TSource> addItemFunc)
        {
            return From(source, (x, y, z) => addItemFunc(x, y));
        }

        public static TDefinition From<TSource>(IEnumerable<TSource> source, Action<TDefinition, TSource, int> addItemFunc)
        {
            Guard.IsNotNull(source, nameof(source));
            Guard.IsNotNull(addItemFunc, nameof(addItemFunc));

            TDefinition type = new TDefinition();
            int index = 0;
            foreach (TSource item in source)
            {
                addItemFunc(type, item, index++);
            }
            return type;
        }
    }

    public abstract class StructuredType<TDefinition, TItem> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem item)
        {
            base.AddItem(item);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2)
        {
            base.AddItem(item1, item2);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3)
        {
            base.AddItem(item1, item2, item3);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4)
        {
            base.AddItem(item1, item2, item3, item4);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5)
        {
            base.AddItem(item1, item2, item3, item4, item5);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10, TItem11> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10, TItem11 item11)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10, TItem11, TItem12> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10, TItem11 item11, TItem12 item12)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10, TItem11, TItem12, TItem13> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10, TItem11 item11, TItem12 item12, TItem13 item13)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10, TItem11, TItem12, TItem13, TItem14, TItem15> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10, TItem11 item11, TItem12 item12, TItem13 item13, TItem14 item14, TItem15 item15)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15);
        }
    }

    public abstract class StructuredType<TDefinition, TItem1, TItem2, TItem3, TItem4, TItem5, TItem6, TItem7, TItem8, TItem9, TItem10, TItem11, TItem12, TItem13, TItem14, TItem15, TItem16, TItem17> : StructuredType<TDefinition> where TDefinition : StructuredType, new()
    {
        protected StructuredType(string typeName) : base(typeName) { }

        protected void AddValues(TItem1 item1, TItem2 item2, TItem3 item3, TItem4 item4, TItem5 item5, TItem6 item6, TItem7 item7, TItem8 item8, TItem9 item9, TItem10 item10, TItem11 item11, TItem12 item12, TItem13 item13, TItem14 item14, TItem15 item15, TItem16 item16, TItem17 item17)
        {
            base.AddItem(item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17);
        }
    }
}