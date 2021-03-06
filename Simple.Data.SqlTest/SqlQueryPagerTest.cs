﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Simple.Data.SqlServer;

namespace Simple.Data.SqlTest
{
    [TestFixture]
    public class SqlQueryPagerTest
    {
        static readonly Regex Normalize = new Regex(@"\s+", RegexOptions.Multiline);

        [Test]
        public void ShouldApplyLimitUsingTop()
        {
            var sql = "select a,b,c from d where a = 1 order by c";
            var expected = new[]{ "select top 5 a,b,c from d where a = 1 order by c"};

            var pagedSql = new SqlQueryPager().ApplyLimit(sql, 5);
            var modified = pagedSql.Select(x => Normalize.Replace(x, " ").ToLowerInvariant());

            Assert.IsTrue(expected.SequenceEqual(modified));
        }

        [Test]
        public void ShouldApplyPagingUsingOrderBy()
        {
            var sql = "select [dbo].[d].[a],[dbo].[d].[b],[dbo].[d].[c] from [dbo].[d] where [dbo].[d].[a] = 1 order by [dbo].[d].[c]";
            var expected = new[]{
                "with __data as (select [dbo].[d].[a], row_number() over(order by [dbo].[d].[c]) as [_#_] from [dbo].[d] where [dbo].[d].[a] = 1)"
                + " select [dbo].[d].[a],[dbo].[d].[b],[dbo].[d].[c] from __data join [dbo].[d] on [dbo].[d].[a] = __data.[a] where [dbo].[d].[a] = 1 and [_#_] between 6 and 15"};

            var pagedSql = new SqlQueryPager().ApplyPaging(sql, new[] {"[dbo].[d].[a]"}, 5, 10);
            var modified = pagedSql.Select(x => Normalize.Replace(x, " ").ToLowerInvariant()).ToArray();

            Assert.AreEqual(expected[0], modified[0]);
        }

        [Test]
        public void ShouldApplyPagingUsingOrderByKeysIfNotAlreadyOrdered()
        {
            var sql = "select [dbo].[d].[a],[dbo].[d].[b],[dbo].[d].[c] from [dbo].[d] where [dbo].[d].[a] = 1";
            var expected = new[]{
                "with __data as (select [dbo].[d].[a], row_number() over(order by [dbo].[d].[a]) as [_#_] from [dbo].[d] where [dbo].[d].[a] = 1)"
                + " select [dbo].[d].[a],[dbo].[d].[b],[dbo].[d].[c] from __data join [dbo].[d] on [dbo].[d].[a] = __data.[a] where [dbo].[d].[a] = 1 and [_#_] between 11 and 30"};

            var pagedSql = new SqlQueryPager().ApplyPaging(sql, new[] {"[dbo].[d].[a]"}, 10, 20);
            var modified = pagedSql.Select(x => Normalize.Replace(x, " ").ToLowerInvariant()).ToArray();

            Assert.AreEqual(expected[0], modified[0]);
        }

        [Test]
        public void ShouldCopeWithAliasedColumns()
        {
            var sql = "select [dbo].[d].[a],[dbo].[d].[b] as [foo],[dbo].[d].[c] from [dbo].[d] where [dbo].[d].[a] = 1";
            var expected =new[]{
                "with __data as (select [dbo].[d].[a], row_number() over(order by [dbo].[d].[a]) as [_#_] from [dbo].[d] where [dbo].[d].[a] = 1)"
                + " select [dbo].[d].[a],[dbo].[d].[b] as [foo],[dbo].[d].[c] from __data join [dbo].[d] on [dbo].[d].[a] = __data.[a] where [dbo].[d].[a] = 1 and [_#_] between 21 and 25"};

            var pagedSql = new SqlQueryPager().ApplyPaging(sql, new[]{"[dbo].[d].[a]"}, 20, 5);
            var modified = pagedSql.Select(x => Normalize.Replace(x, " ").ToLowerInvariant()).ToArray();

            Assert.AreEqual(expected[0], modified[0]);
        }
    }
}
