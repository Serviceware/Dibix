using System;

namespace Dibix.Dapper
{
    internal abstract class DapperMappingCheck
    {
        internal void Check<T>() => this.Check(typeof(T));
        internal void Check<TFirst, TSecond>() => this.Check(typeof(TFirst), typeof(TSecond));
        internal void Check<TFirst, TSecond, TThird>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird));
        internal void Check<TFirst, TSecond, TThird, TFourth>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth));
        protected abstract void Check(params Type[] types);
    }
}