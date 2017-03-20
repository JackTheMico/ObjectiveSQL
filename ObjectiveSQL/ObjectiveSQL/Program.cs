using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ObjectiveSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            testSelect();
            testUpdate();
            testInsert();
            testDelete();
            try
            {
                testInsertWhere();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        static void testSelect()
        {
            string usernamePrefix = "JOHN";
            string role = "admin";
            string username = "admin";

            Command command = SQL.SELECT("*").From("USERS")
                            .Where("USERNAME LIKE ?", usernamePrefix)
                            .And("(ROLE=?", role).Or("USERNAME=?)", username).toCommand();
            Console.WriteLine(command.getStatement());

            command = SQL.SELECT("*").From("USERS")
                            .Where("USERNAME LIKE ?", usernamePrefix)
                            .Append("AND (")
                            .Append(role != null, "ROLE = ?", role)
                            .Or("USERNAME = ?", username)
                            .Append(")").toCommand();
            Console.WriteLine(command.getStatement());

            role = null;
            command = SQL.SELECT("COUNT(1), ROLE")
                            .From("USERS")
                            .Where(false, "REGISTER_TIME > sysdate - 1")    // dismissed
                            .AndIfNotEmpty("ROLE = ?", role)          // dismissed
                            .And("1=1")
                            .GroupBy("ROLE").toCommand();
            Console.WriteLine(command.getStatement());


            List<string> levels = new List<string>() { "1","2", "3" };
            command = SQL.SELECT("*")
                            .From("USERS")
                            .Where("USER_LEVEL IN ?", levels).toCommand();

            Console.WriteLine(command.getStatement());
        }

        static void testUpdate()
        {
            Command command = SQL.UPDATE("USER")
                            .Set("AGE", 3)
                            .Set(false, "NAME", "admin").Where("ID=?", 1).toCommand();
            Console.WriteLine(command.getStatement());
        }

        static void testInsert()
        {
            Command command = SQL.INSERT("USER")
                            .Values("ID", 1)
                            .Values("USERNAME", "admin")
                            .Values("PASSWORD", "admin")
                            .Values("AGE", null).toCommand();
            Console.WriteLine(command.getStatement());

            Dictionary<string,object> test = new Dictionary<string,object>();
            test["ID"] = 1;
            test["ADMIN"] = "Jack";
            test["PWD"] = "123456";
            Command comText = SQL.INSERT("USER").Values(test).toCommand();
            Console.WriteLine("Dictionary--" + comText.getStatement());
        }

        static void testInsertWhere()
        {
            Command command = SQL.INSERT("USER").Values("NAME", "admin").Where("").toCommand();
            Console.WriteLine(command.getStatement());
        }

        static void testDelete()
        {
            Command command = SQL.DELETE("USER").Where("ID in ?", new List<string>() { "1", "2", "3", "4", "5" }).toCommand();
            Console.WriteLine(command.getStatement());
        }
    }
}
