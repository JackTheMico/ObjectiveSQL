using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectiveSQL
{
    /// <summary>
    /// 生成 Command 的帮助类
    /// </summary>
    public class SQL
    {
        private static Boolean isEmpty(object obj)
        {
            if (obj == null)
                return true;

            string str = obj.ToString();
            return str.Length == 0 || str.Trim().Length == 0;
        }

        public static Select SELECT(string columns)
        {
            return new Select(columns);
        }

        public static Update UPDATE(string table)
        {
            return new Update(table);
        }

        public static Insert INSERT(string table)
        {
            return new Insert(table);
        }

        public static Delete DELETE(string table)
        {
            return new Delete(table);
        }

        public enum Pref
        {
            AND, OR
        }

        public class Pair
        {

            public Pref pref { get; set; }

            public string name { get; set; }

            public object value { get; set; }

            public Pair(string name, object value)
            {
                this.name = name;
                this.value = value;
            }

            public Pair(Pref pref, string name, object value)
            {
                this.pref = pref;
                this.name = name;
                this.value = value;
            }
        }

        class StatementPair : Pair
        {

            public StatementPair(string statement)
                : base(statement, null)
            {

            }

            public StatementPair(Pref pref, string statement)
                : base(pref, statement, null)
            {

            }
        }

        /////////////////////////////////////////////////////////

        public abstract class Generatable<T> where T : Generatable<T>
        {

            protected string table;

            protected string statement;

            protected List<string> @params = new List<string>();

            protected List<Pair> conditions = new List<Pair>();

            public abstract Command toCommand();

            public string joinNames(List<Pair> pairs)
            {
                if (pairs.Count == 0)
                    return "";
                else
                {
                    string result = "";
                    foreach (Pair pair in pairs)
                    {
                        result += pair.name + ",";
                    }
                    result = result.Substring(0, result.Length - 1);
                    return result;
                }
            }

            protected string joinQuestionMarks(List<Pair> pairs)
            {
                StringBuilder s = new StringBuilder();
                for (int size = pairs.Count, i = 0; i < size; i++)
                    s.Append(pairs[i].value.ToString()).Append(i == size - 1 ? "" : ",");
                return s.ToString();
            }

            protected List<string> joinValues(List<Pair> pairs)
            {
                if (pairs.Count == 0)
                    return new List<string>();

                List<string> result = new List<string>();
                foreach (Pair pair in pairs)
                    result.Add(pair.value.ToString());

                return result;
            }

            public T Where(string statement)
            {
                if (this is Insert)
                    throw new Exception("cannot use 'where' block in Insert");
                this.conditions.Add(new StatementPair(statement));
                return (T)this;
            }

            public T Where(string column, object value)
            {
                if (this is Insert)
                {
                    throw new Exception("cannot use 'where' block in Insert");
                }
                this.conditions.Add(new Pair(column, value));
                return (T)this;
            }

            public T Where(Boolean exp, string statement)
            {
                if (this is Insert)
                    throw new Exception("cannot use 'where' block in Insert");
                if (exp)
                    this.conditions.Add(new StatementPair(statement));
                return (T)this;
            }

            public T Where(Boolean exp, string column, object value)
            {
                if (this is Insert)
                    throw new Exception("cannot use 'where' block in Insert");
                if (exp)
                    this.conditions.Add(new Pair(column, value));
                return (T)this;
            }

            public T And(string statement)
            {
                this.conditions.Add(new StatementPair(Pref.AND, statement));
                return (T)this;
            }

            public T And(string column, object value)
            {
                this.conditions.Add(new Pair(Pref.AND, column, value));
                return (T)this;
            }

            public T And(Boolean exp, string statement)
            {
                if (exp)
                {
                    this.conditions.Add(new StatementPair(Pref.AND, statement));
                }
                return (T)this;
            }

            public T And(Boolean exp, string column, object value)
            {
                if (exp)
                {
                    this.conditions.Add(new Pair(Pref.AND, column, value));
                }
                return (T)this;
            }

            public T AndIfNotEmpty(string column, object value)
            {
                return And(!isEmpty(value), column, value);
            }

            public T Or(string statement)
            {
                this.conditions.Add(new StatementPair(Pref.OR, statement));
                return (T)this;
            }

            public T Or(string column, object value)
            {
                this.conditions.Add(new Pair(Pref.OR, column, value));
                return (T)this;
            }

            public T Or(Boolean exp, string statement)
            {
                if (exp)
                {
                    this.conditions.Add(new StatementPair(Pref.OR, statement));
                }
                return (T)this;
            }

            public T Or(Boolean exp, string column, object value)
            {
                if (exp)
                    this.conditions.Add(new Pair(Pref.OR, column, value));
                return (T)this;
            }

            public T OrIfNotEmpty(string column, object value)
            {
                return Or(!isEmpty(value), column, value);
            }

            public T Append(string statement)
            {
                this.conditions.Add(new StatementPair(statement));
                return (T)this;
            }

            public T Append(string column, object value)
            {
                this.conditions.Add(new Pair(column, value));
                return (T)this;
            }

            public T Append(Boolean exp, string statement)
            {
                if (exp)
                    this.conditions.Add(new StatementPair(statement));
                return (T)this;
            }

            public T Append(Boolean exp, string column, object value)
            {
                if (exp)
                    this.conditions.Add(new Pair(column, value));
                return (T)this;
            }

            protected string generateWhereBlock()
            {
                string where = "";

                if (this.conditions.Count > 0)
                {
                    where = "WHERE ";

                    for (int i = 0, conditionsSize = conditions.Count; i < conditionsSize; i++)
                    {
                        Pair condition = conditions[i];
                        where = processCondition(i, where, condition);
                    }

                }

                return " " + where;
            }

            private string processCondition(int index, string where, Pair condition)
            {

                where = where.Trim();

                // 第一个条件不能加 and 和 or 前缀
                if (index > 0 && !where.EndsWith("("))
                {
                    if (condition.pref == Pref.AND)
                        where += " AND ";
                    else if (condition.pref == Pref.OR)
                        where += " OR ";
                }

                where += " ";

                if (condition is StatementPair)
                {       // 不带参数的条件
                    where += condition.name;

                }
                else if (condition.value is List<string>)
                {   // 参数为 List 的条件（即 in 条件）
                    string marks = "(";

                    foreach (string o in (List<string>)condition.value)
                    {
                        marks += o+",";
                        this.@params.Add(o);
                    }

                    if (marks.EndsWith(","))
                    {
                        marks = marks.Substring(0, marks.Length - 1);
                    }
                    marks += ")";                                 // marks = "(value1,value2,value3,...,valueEnd)"

                    where += condition.name.Replace("?", marks);  // "A in ?" -> "A in (value1,value2,value3)"

                }
                else
                {
                   // where += condition.name;
                    where += condition.name.Replace("?", condition.value.ToString());
                    this.@params.Add(condition.value.ToString());
                }

                return where;
            }
        }

        /////////////////////////////////////////////////////////

        public class Insert : Generatable<Insert>
        {
            private List<Pair> pairs = new List<Pair>();

            public Insert(string table)
            {
                this.table = table;
            }

            public Insert Values(string column, object value)
            {
                if (value != null)
                    pairs.Add(new Pair(column, value));
                return this;
            }

            public Insert Values(Boolean ifTrue, string column, object value)
            {
                if (ifTrue)
                    Values(column, value);
                return this;
            }

            public Insert Values(Dictionary<string, object> map)
            {
                foreach (KeyValuePair<string, object> entry in map.AsEnumerable())
                    Values(entry.Key, entry.Value);
                return this;
            }

            public override Command toCommand()
            {
                this.statement = "INSERT INTO " + table + "(" + joinNames(pairs) + ") VALUES (" + joinQuestionMarks(pairs) + ")";
                this.@params = joinValues(pairs);

                return new Command(statement, @params);
            }
        }

        /////////////////////////////////////////////////////////

        /**
         * 用于生成 update 语句的帮助类
         */
        public class Update : Generatable<Update>
        {

            private List<Pair> updates = new List<Pair>();

            public Update(string table)
            {
                this.table = table;
            }

            public override Command toCommand()
            {
                this.statement = "UPDATE " + table +
                        " set " + generateSetBlock() + " " + generateWhereBlock();

                return new Command(this.statement, this.@params);
            }

            private string generateSetBlock()
            {
                string statement = "";

                for (int i = 0, updatesSize = updates.Count; i < updatesSize; i++)
                {
                    Pair pair = updates[i];
                    if (pair is StatementPair)
                        statement += pair.name;
                    else
                    {
                        this.@params.Add(pair.value.ToString());
                        statement += pair.name + " = '"+pair.value.ToString()+"'";
                    }

                    if (i < updatesSize - 1)
                    {
                        statement += ",";
                    }
                }

                return statement;
            }

            public Update Set(Boolean exp, string column, object value)
            {
                if (exp)
                {
                    this.updates.Add(new Pair(column, value));
                }
                return this;
            }

            public Update Set(string column, object value)
            {
                this.updates.Add(new Pair(column, value));
                return this;
            }

            public Update Set(string setStatement)
            {
                this.updates.Add(new StatementPair(setStatement));
                return this;
            }

            public Update Set(Boolean exp, string setStatement)
            {
                if (exp)
                    this.updates.Add(new StatementPair(setStatement));
                return this;
            }

            public Update SetIfNotNull(string column, object value)
            {
                return Set(value != null, column, value);
            }

            public Update SetIfNotEmpty(string column, object value)
            {
                return Set(!isEmpty(value), column, value);
            }
        }

        /////////////////////////////////////////////////////////

        /// <summary>
        /// 用于生成 select 语句的帮助类
        /// </summary>
        public class Select : Generatable<Select>
        {

            private string columns;

            private string from;

            private string orderBy;

            private string groupBy;

            public Select(string columns)
            {
                this.columns = columns;
            }

            public Select From(string from)
            {
                this.from = from;
                return this;
            }

            public Select OrderBy(string orderBy)
            {
                this.orderBy = orderBy;
                return this;
            }

            public Select GroupBy(string groupBy)
            {
                this.groupBy = groupBy;
                return this;
            }


            public override Command toCommand()
            {
                this.statement = "SELECT " + this.columns + " FROM " + this.from + " ";

                this.statement += generateWhereBlock();

                if (!isEmpty(this.groupBy))
                    this.statement += " GROUP BY " + this.groupBy;

                if (!isEmpty(this.orderBy))
                    this.statement += " ORDER BY " + this.orderBy;

                return new Command(this.statement, this.@params);
            }
        }

        public class Delete : Generatable<Delete>
        {
            public Delete(string table)
            {
                this.table = table;
            }

            public override Command toCommand()
            {
                this.statement = "DELETE FROM " + table + generateWhereBlock();
                return new Command(this.statement, this.@params);
            }
        }
    }
}