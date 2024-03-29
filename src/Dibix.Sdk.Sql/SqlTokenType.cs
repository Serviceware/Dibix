﻿using System;
using System.Runtime.CompilerServices;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    /// <remarks>
    /// Starting with version 15.0.4532.1 of the SSDT/Microsoft.SqlServer.TransactSql.ScriptDom a new member
    /// TSqlTokenType.Rename was introduced in the middle, which unfortunately causes most of the members to get a new values.
    /// Since the assembly version itself is not changed, both our assembly and the one used by a (newer) VS/MSBuild have the same identity.
    /// Therefore we cannot force working with our (older) assembly. This is the result => a very messy workaround.
    /// </remarks>
    /// <see cref="https://www.fuget.org/packages/Microsoft.SqlServer.DacFx.x64/150.4532.1/lib/net46/diff/150.4384.2/" />
    public class SqlTokenType
    {
        private readonly string _name;

        public static SqlTokenType EndOfFile => new SqlTokenType();
		public static SqlTokenType None => new SqlTokenType();
		public static SqlTokenType Add => new SqlTokenType();
		public static SqlTokenType All => new SqlTokenType();
		public static SqlTokenType Alter => new SqlTokenType();
		public static SqlTokenType And => new SqlTokenType();
		public static SqlTokenType Any => new SqlTokenType();
		public static SqlTokenType As => new SqlTokenType();
		public static SqlTokenType Asc => new SqlTokenType();
		public static SqlTokenType Authorization => new SqlTokenType();
		public static SqlTokenType Backup => new SqlTokenType();
		public static SqlTokenType Begin => new SqlTokenType();
		public static SqlTokenType Between => new SqlTokenType();
		public static SqlTokenType Break => new SqlTokenType();
		public static SqlTokenType Browse => new SqlTokenType();
		public static SqlTokenType Bulk => new SqlTokenType();
		public static SqlTokenType By => new SqlTokenType();
		public static SqlTokenType Cascade => new SqlTokenType();
		public static SqlTokenType Case => new SqlTokenType();
		public static SqlTokenType Check => new SqlTokenType();
		public static SqlTokenType Checkpoint => new SqlTokenType();
		public static SqlTokenType Close => new SqlTokenType();
		public static SqlTokenType Clustered => new SqlTokenType();
		public static SqlTokenType Coalesce => new SqlTokenType();
		public static SqlTokenType Collate => new SqlTokenType();
		public static SqlTokenType Column => new SqlTokenType();
		public static SqlTokenType Commit => new SqlTokenType();
		public static SqlTokenType Compute => new SqlTokenType();
		public static SqlTokenType Constraint => new SqlTokenType();
		public static SqlTokenType Contains => new SqlTokenType();
		public static SqlTokenType ContainsTable => new SqlTokenType();
		public static SqlTokenType Continue => new SqlTokenType();
		public static SqlTokenType Convert => new SqlTokenType();
		public static SqlTokenType Create => new SqlTokenType();
		public static SqlTokenType Cross => new SqlTokenType();
		public static SqlTokenType Current => new SqlTokenType();
		public static SqlTokenType CurrentDate => new SqlTokenType();
		public static SqlTokenType CurrentTime => new SqlTokenType();
		public static SqlTokenType CurrentTimestamp => new SqlTokenType();
		public static SqlTokenType CurrentUser => new SqlTokenType();
		public static SqlTokenType Cursor => new SqlTokenType();
		public static SqlTokenType Database => new SqlTokenType();
		public static SqlTokenType Dbcc => new SqlTokenType();
		public static SqlTokenType Deallocate => new SqlTokenType();
		public static SqlTokenType Declare => new SqlTokenType();
		public static SqlTokenType Default => new SqlTokenType();
		public static SqlTokenType Delete => new SqlTokenType();
		public static SqlTokenType Deny => new SqlTokenType();
		public static SqlTokenType Desc => new SqlTokenType();
		public static SqlTokenType Distinct => new SqlTokenType();
		public static SqlTokenType Distributed => new SqlTokenType();
		public static SqlTokenType Double => new SqlTokenType();
		public static SqlTokenType Drop => new SqlTokenType();
		public static SqlTokenType Else => new SqlTokenType();
		public static SqlTokenType End => new SqlTokenType();
		public static SqlTokenType Errlvl => new SqlTokenType();
		public static SqlTokenType Escape => new SqlTokenType();
		public static SqlTokenType Except => new SqlTokenType();
		public static SqlTokenType Exec => new SqlTokenType();
		public static SqlTokenType Execute => new SqlTokenType();
		public static SqlTokenType Exists => new SqlTokenType();
		public static SqlTokenType Exit => new SqlTokenType();
		public static SqlTokenType Fetch => new SqlTokenType();
		public static SqlTokenType File => new SqlTokenType();
		public static SqlTokenType FillFactor => new SqlTokenType();
		public static SqlTokenType For => new SqlTokenType();
		public static SqlTokenType Foreign => new SqlTokenType();
		public static SqlTokenType FreeText => new SqlTokenType();
		public static SqlTokenType FreeTextTable => new SqlTokenType();
		public static SqlTokenType From => new SqlTokenType();
		public static SqlTokenType Full => new SqlTokenType();
		public static SqlTokenType Function => new SqlTokenType();
		public static SqlTokenType GoTo => new SqlTokenType();
		public static SqlTokenType Grant => new SqlTokenType();
		public static SqlTokenType Group => new SqlTokenType();
		public static SqlTokenType Having => new SqlTokenType();
		public static SqlTokenType HoldLock => new SqlTokenType();
		public static SqlTokenType Identity => new SqlTokenType();
		public static SqlTokenType IdentityInsert => new SqlTokenType();
		public static SqlTokenType IdentityColumn => new SqlTokenType();
		public static SqlTokenType If => new SqlTokenType();
		public static SqlTokenType In => new SqlTokenType();
		public static SqlTokenType Index => new SqlTokenType();
		public static SqlTokenType Inner => new SqlTokenType();
		public static SqlTokenType Insert => new SqlTokenType();
		public static SqlTokenType Intersect => new SqlTokenType();
		public static SqlTokenType Into => new SqlTokenType();
		public static SqlTokenType Is => new SqlTokenType();
		public static SqlTokenType Join => new SqlTokenType();
		public static SqlTokenType Key => new SqlTokenType();
		public static SqlTokenType Kill => new SqlTokenType();
		public static SqlTokenType Left => new SqlTokenType();
		public static SqlTokenType Like => new SqlTokenType();
		public static SqlTokenType LineNo => new SqlTokenType();
		public static SqlTokenType National => new SqlTokenType();
		public static SqlTokenType NoCheck => new SqlTokenType();
		public static SqlTokenType NonClustered => new SqlTokenType();
		public static SqlTokenType Not => new SqlTokenType();
		public static SqlTokenType Null => new SqlTokenType();
		public static SqlTokenType NullIf => new SqlTokenType();
		public static SqlTokenType Of => new SqlTokenType();
		public static SqlTokenType Off => new SqlTokenType();
		public static SqlTokenType Offsets => new SqlTokenType();
		public static SqlTokenType On => new SqlTokenType();
		public static SqlTokenType Open => new SqlTokenType();
		public static SqlTokenType OpenDataSource => new SqlTokenType();
		public static SqlTokenType OpenQuery => new SqlTokenType();
		public static SqlTokenType OpenRowSet => new SqlTokenType();
		public static SqlTokenType OpenXml => new SqlTokenType();
		public static SqlTokenType Option => new SqlTokenType();
		public static SqlTokenType Or => new SqlTokenType();
		public static SqlTokenType Order => new SqlTokenType();
		public static SqlTokenType Outer => new SqlTokenType();
		public static SqlTokenType Over => new SqlTokenType();
		public static SqlTokenType Percent => new SqlTokenType();
		public static SqlTokenType Plan => new SqlTokenType();
		public static SqlTokenType Primary => new SqlTokenType();
		public static SqlTokenType Print => new SqlTokenType();
		public static SqlTokenType Proc => new SqlTokenType();
		public static SqlTokenType Procedure => new SqlTokenType();
		public static SqlTokenType Public => new SqlTokenType();
		public static SqlTokenType Raiserror => new SqlTokenType();
		public static SqlTokenType Read => new SqlTokenType();
		public static SqlTokenType ReadText => new SqlTokenType();
		public static SqlTokenType Reconfigure => new SqlTokenType();
		public static SqlTokenType References => new SqlTokenType();
		public static SqlTokenType Rename => new SqlTokenType();
		public static SqlTokenType Replication => new SqlTokenType();
		public static SqlTokenType Restore => new SqlTokenType();
		public static SqlTokenType Restrict => new SqlTokenType();
		public static SqlTokenType Return => new SqlTokenType();
		public static SqlTokenType Revoke => new SqlTokenType();
		public static SqlTokenType Right => new SqlTokenType();
		public static SqlTokenType Rollback => new SqlTokenType();
		public static SqlTokenType RowCount => new SqlTokenType();
		public static SqlTokenType RowGuidColumn => new SqlTokenType();
		public static SqlTokenType Rule => new SqlTokenType();
		public static SqlTokenType Save => new SqlTokenType();
		public static SqlTokenType Schema => new SqlTokenType();
		public static SqlTokenType Select => new SqlTokenType();
		public static SqlTokenType SessionUser => new SqlTokenType();
		public static SqlTokenType Set => new SqlTokenType();
		public static SqlTokenType SetUser => new SqlTokenType();
		public static SqlTokenType Shutdown => new SqlTokenType();
		public static SqlTokenType Some => new SqlTokenType();
		public static SqlTokenType Statistics => new SqlTokenType();
		public static SqlTokenType SystemUser => new SqlTokenType();
		public static SqlTokenType Table => new SqlTokenType();
		public static SqlTokenType TextSize => new SqlTokenType();
		public static SqlTokenType Then => new SqlTokenType();
		public static SqlTokenType To => new SqlTokenType();
		public static SqlTokenType Top => new SqlTokenType();
		public static SqlTokenType Tran => new SqlTokenType();
		public static SqlTokenType Transaction => new SqlTokenType();
		public static SqlTokenType Trigger => new SqlTokenType();
		public static SqlTokenType Truncate => new SqlTokenType();
		public static SqlTokenType TSEqual => new SqlTokenType();
		public static SqlTokenType Union => new SqlTokenType();
		public static SqlTokenType Unique => new SqlTokenType();
		public static SqlTokenType Update => new SqlTokenType();
		public static SqlTokenType UpdateText => new SqlTokenType();
		public static SqlTokenType Use => new SqlTokenType();
		public static SqlTokenType User => new SqlTokenType();
		public static SqlTokenType Values => new SqlTokenType();
		public static SqlTokenType Varying => new SqlTokenType();
		public static SqlTokenType View => new SqlTokenType();
		public static SqlTokenType WaitFor => new SqlTokenType();
		public static SqlTokenType When => new SqlTokenType();
		public static SqlTokenType Where => new SqlTokenType();
		public static SqlTokenType While => new SqlTokenType();
		public static SqlTokenType With => new SqlTokenType();
		public static SqlTokenType WriteText => new SqlTokenType();
		public static SqlTokenType Disk => new SqlTokenType();
		public static SqlTokenType Precision => new SqlTokenType();
		public static SqlTokenType External => new SqlTokenType();
		public static SqlTokenType Revert => new SqlTokenType();
		public static SqlTokenType Pivot => new SqlTokenType();
		public static SqlTokenType Unpivot => new SqlTokenType();
		public static SqlTokenType TableSample => new SqlTokenType();
		public static SqlTokenType Dump => new SqlTokenType();
		public static SqlTokenType Load => new SqlTokenType();
		public static SqlTokenType Merge => new SqlTokenType();
		public static SqlTokenType StopList => new SqlTokenType();
		public static SqlTokenType SemanticKeyPhraseTable => new SqlTokenType();
		public static SqlTokenType SemanticSimilarityTable => new SqlTokenType();
		public static SqlTokenType SemanticSimilarityDetailsTable => new SqlTokenType();
		public static SqlTokenType TryConvert => new SqlTokenType();
		public static SqlTokenType Bang => new SqlTokenType();
		public static SqlTokenType PercentSign => new SqlTokenType();
		public static SqlTokenType Ampersand => new SqlTokenType();
		public static SqlTokenType LeftParenthesis => new SqlTokenType();
		public static SqlTokenType RightParenthesis => new SqlTokenType();
		public static SqlTokenType LeftCurly => new SqlTokenType();
		public static SqlTokenType RightCurly => new SqlTokenType();
		public static SqlTokenType Star => new SqlTokenType();
		public static SqlTokenType MultiplyEquals => new SqlTokenType();
		public static SqlTokenType Plus => new SqlTokenType();
		public static SqlTokenType Comma => new SqlTokenType();
		public static SqlTokenType Minus => new SqlTokenType();
		public static SqlTokenType Dot => new SqlTokenType();
		public static SqlTokenType Divide => new SqlTokenType();
		public static SqlTokenType Colon => new SqlTokenType();
		public static SqlTokenType DoubleColon => new SqlTokenType();
		public static SqlTokenType Semicolon => new SqlTokenType();
		public static SqlTokenType LessThan => new SqlTokenType();
		public static SqlTokenType EqualsSign => new SqlTokenType();
		public static SqlTokenType RightOuterJoin => new SqlTokenType();
		public static SqlTokenType GreaterThan => new SqlTokenType();
		public static SqlTokenType Circumflex => new SqlTokenType();
		public static SqlTokenType VerticalLine => new SqlTokenType();
		public static SqlTokenType Tilde => new SqlTokenType();
		public static SqlTokenType AddEquals => new SqlTokenType();
		public static SqlTokenType SubtractEquals => new SqlTokenType();
		public static SqlTokenType DivideEquals => new SqlTokenType();
		public static SqlTokenType ModEquals => new SqlTokenType();
		public static SqlTokenType BitwiseAndEquals => new SqlTokenType();
		public static SqlTokenType BitwiseOrEquals => new SqlTokenType();
		public static SqlTokenType BitwiseXorEquals => new SqlTokenType();
		public static SqlTokenType Go => new SqlTokenType();
		public static SqlTokenType Label => new SqlTokenType();
		public static SqlTokenType Integer => new SqlTokenType();
		public static SqlTokenType Numeric => new SqlTokenType();
		public static SqlTokenType Real => new SqlTokenType();
		public static SqlTokenType HexLiteral => new SqlTokenType();
		public static SqlTokenType Money => new SqlTokenType();
		public static SqlTokenType SqlCommandIdentifier => new SqlTokenType();
		public static SqlTokenType PseudoColumn => new SqlTokenType();
		public static SqlTokenType DollarPartition => new SqlTokenType();
		public static SqlTokenType AsciiStringOrQuotedIdentifier => new SqlTokenType();
		public static SqlTokenType AsciiStringLiteral => new SqlTokenType();
		public static SqlTokenType UnicodeStringLiteral => new SqlTokenType();
		public static SqlTokenType Identifier => new SqlTokenType();
		public static SqlTokenType QuotedIdentifier => new SqlTokenType();
		public static SqlTokenType Variable => new SqlTokenType();
		public static SqlTokenType OdbcInitiator => new SqlTokenType();
		public static SqlTokenType ProcNameSemicolon => new SqlTokenType();
		public static SqlTokenType SingleLineComment => new SqlTokenType();
		public static SqlTokenType MultilineComment => new SqlTokenType();
		public static SqlTokenType WhiteSpace => new SqlTokenType();

        private SqlTokenType([CallerMemberName] string name = null) => this._name = name;

        public static implicit operator TSqlTokenType(SqlTokenType type) => (TSqlTokenType)Enum.Parse(typeof(TSqlTokenType), type._name);
    }
}
