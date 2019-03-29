using System;

namespace Dibix.Dapper
{
    internal abstract class DapperMappingCheck
    {
        internal void Check<T>() => this.Check(typeof(T));
        internal void Check<TFirst, TSecond>() => this.Check(typeof(TFirst), typeof(TSecond));
        internal void Check<TFirst, TSecond, TThird>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird));
        internal void Check<TFirst, TSecond, TThird, TFourth>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth));
        internal void Check<TFirst, TSecond, TThird, TFourth, TFifth>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth));
        internal void Check<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth));
        internal void Check<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TEighth, TNinth, TTenth, TEleventh>() => this.Check(typeof(TFirst), typeof(TSecond), typeof(TThird), typeof(TFourth), typeof(TFifth), typeof(TSixth), typeof(TSeventh), typeof(TEighth), typeof(TNinth), typeof(TTenth), typeof(TEleventh));
        protected abstract void Check(params Type[] types);
    }
}