
//
//  Multi-user  BikeHike  Windows  app,  using  transactions.
//
//  KRISTI GJONI
//  U.  of  Illinois,  Chicago
//  CS480,  Summer  2018
//  Project  #3
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateDBApp
{
  class Program
  {

    static void Main(string[] args)
    {
      Console.WriteLine();
      Console.WriteLine("** Create Database Console App **");
      Console.WriteLine();

      string baseDatabaseName = "BikeHike";
      string sql;

      try
      {
        //
        // 1. Make a copy of empty MDF file to get us started:
        //
        Console.WriteLine("Copying empty database to {0}.mdf and {0}_log.ldf...", baseDatabaseName);

        CopyEmptyFile("__EmptyDB", baseDatabaseName);

        Console.WriteLine();

        //
        // 2. Now let's make sure we can connect to SQL Server on local machine:
        //
        DataAccessTier.Data data = new DataAccessTier.Data(baseDatabaseName + ".mdf");

        Console.Write("Testing access to database: ");

        if (data.TestConnection())
          Console.WriteLine("success");
        else
          Console.WriteLine("failure?!");

        Console.WriteLine();

        //
        // 3. Create tables by reading from .sql file and executing DDL queries:
        //
        Console.WriteLine("Creating tables by executing {0}.sql file...", baseDatabaseName);

        string[] lines = System.IO.File.ReadAllLines(baseDatabaseName + ".sql");

        sql = "";

        for (int i = 0; i < lines.Length; ++i)
        {
          string next = lines[i];

          if (next.Trim() == "")  // empty line, ignore...
          {
          }
          else if (next.Contains(";"))  // we have found the end of the query:
          {
            sql = sql + next + System.Environment.NewLine;

            Console.WriteLine("** Executing '{0}'...", sql.Substring(0, 32));

            data.ExecuteActionQuery(sql);

            sql = "";  // reset:
          }
          else  // add to existing query:
          {
              sql = sql + next + System.Environment.NewLine;
          }
        }

        Console.WriteLine();

        //
        // 4. Insert data by parsing data from .csv files:
        //
        Console.WriteLine("Inserting data...");

        //
        // 4 a. Inserting data for biketypes
        //
        Console.WriteLine("**Insert bike types...");

         using (var file = new System.IO.StreamReader("biketypes.csv"))
         {
               while (!file.EndOfStream)
               {
                    string line = file.ReadLine();
                    string[] values = line.Split(',');
                    string description = values[1];
                    double priceperhour = Convert.ToDouble(values[2]);
                    string sqlq = string.Format(@"
INSERT INTO BikeTypes(Description, PricePerHour) 
Values('{0}', {1});
", 
description, priceperhour);

                    data.ExecuteActionQuery(sqlq);
               }
         }

        //
        // 4 b. Inserting data for bikes
        //
        Console.WriteLine("**Insert bikes...");

        using (var file = new System.IO.StreamReader("bikes.csv"))
         {
               while (!file.EndOfStream)
               {
                    string line = file.ReadLine();
                    string[] values = line.Split(',');
                    int btypeid = Convert.ToInt32(values[1]);
                    int pyear = Convert.ToInt32(values[2]);
                    string sqlq = string.Format(@"
INSERT INTO Bikes(TID, Year, Rented)
Values({0}, {1}, 0);
",
btypeid, pyear);

                    data.ExecuteActionQuery(sqlq);
               }
         }

        //
        // 4 c. Inserting data for customers
        //
        Console.WriteLine("**Insert customers...");

        using (var file = new System.IO.StreamReader("customers.csv"))
        {
              while (!file.EndOfStream)
              {
                    string line = file.ReadLine();
                    string[] values = line.Split(',');
                    string fname = values[1];
                    string lname = values[2];
                    string cemail = values[3];
                    string sqlq = string.Format(@"
INSERT INTO Customers(FirstName, LastName, Email) 
Values('{0}', '{1}', '{2}');
", 
fname, lname, cemail);

                    data.ExecuteActionQuery(sqlq);
                       
              }
        }

        //
        // Done
        //
      }
      catch (Exception ex)
      {
        Console.WriteLine("**Exception: '{0}'", ex.Message);
      }

      Console.WriteLine();
      Console.WriteLine("** Done **");
      Console.WriteLine();
    }//Main


    /// <summary>
    /// Makes a copy of an existing Microsoft SQL Server database file 
    /// and log file.  Throws an exception if an error occurs, otherwise
    /// returns normally upon successful copying.  Assumes files are in
    /// sub-folder bin\Debug or bin\Release --- i.e. same folder as .exe.
    /// </summary>
    /// <param name="basenameFrom">base file name to copy from</param>
    /// <param name="basenameTo">base file name to copy to</param>
    static void CopyEmptyFile(string basenameFrom, string basenameTo)
    {
      string from_file, to_file;

      //
      // copy .mdf:
      //
      from_file = basenameFrom + ".mdf";
      to_file = basenameTo + ".mdf";

      if (System.IO.File.Exists(to_file))
      {
        System.IO.File.Delete(to_file);
      }

      System.IO.File.Copy(from_file, to_file);

      // 
      // now copy .ldf:
      //
      from_file = basenameFrom + "_log.ldf";
      to_file = basenameTo + "_log.ldf";

      if (System.IO.File.Exists(to_file))
      {
        System.IO.File.Delete(to_file);
      }

      System.IO.File.Copy(from_file, to_file);
    }

  }//class
}//namespace

