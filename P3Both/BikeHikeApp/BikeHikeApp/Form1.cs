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
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace BikeHikeApp
{
  public partial class Form1 : Form
  {
    public readonly string dbfilename = "BikeHike.mdf";

    public Form1()
    {
      InitializeComponent();
    }

    // Adding class RentalCart
    class RentalCart
    {
            int bike_id;
            int cid;

            RentalCart()
            {

            }
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      //
      // As app starts up, ping SQL Server to start it running:
      //
      DataAccessTier.Data datatier = new DataAccessTier.Data(dbfilename);

      try
      {
        datatier.TestConnection();
      }
      catch
      {
        // ignore since just pinging to start:
      }
    }

    private void ClearCustomerInfo()
    {
      this.txtCID.Clear();
      this.txtEmail.Clear();
      this.txtCustomerOnRental.Clear();
      this.txtCustRentalInfo1.Clear();
      this.txtCustRentalInfo2.Clear();
    }


    //
    // List all the customers:
    //
    private void cmdLoadCustomers_Click(object sender, EventArgs e)
    {
      this.lstCustomers.Items.Clear();
      ClearCustomerInfo();

      string sql = string.Format(@"
Select CID, LastName, FirstName, Email
From Customers WITH (INDEX(Customers_FirstName))
Order by Lastname ASC, FirstName ASC;
");

      //MessageBox.Show(sql);

      DataAccessTier.Data datatier = new DataAccessTier.Data(dbfilename);

      try
      {
        DataSet ds = datatier.ExecuteNonScalarQuery(sql);

        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          Customer c = new Customer(
            Convert.ToInt32(row["CID"]),
            row["LastName"].ToString(),
            row["FirstName"].ToString(),
            row["Email"].ToString());

          this.lstCustomers.Items.Add(c);
        }

      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }


    //
    // User has clicked on a customer in the list box, so display information
    // about this customer:
    //
    private void lstCustomers_SelectedIndexChanged(object sender, EventArgs e)
    {
      ClearCustomerInfo();

      if (this.lstCustomers.SelectedIndex < 0)  // nothing selected:
        return;

      //
      // retrieve selected customer object from list box:
      //
      Customer c = (Customer)this.lstCustomers.SelectedItem;

      this.txtCID.Text = c.CID.ToString();
      this.txtEmail.Text = c.Email;

      //
      // is customer out with a rental?
      //
      int RID, N;
      DateTime start;
      double expDuration;

      if (!c.OnRental(out RID, out start, out expDuration, out N))  // no...
      {
        this.txtCustomerOnRental.Text = "customer *not* on a rental";
        return;
      }

      //
      // customer is on a rental, so we need to display # of bikes and 
      // expected return date and time:
      //
      this.txtCustomerOnRental.Text = string.Format("customer is on rental (RID={0})", RID);

      DateTime dueback = start.AddHours(expDuration);

      this.txtCustRentalInfo1.Text = string.Format(
        "# bikes: {0}", N);
      this.txtCustRentalInfo2.Text = string.Format(
        "due: {0}", dueback);
    }


    private void ClearBikeInfo()
    {
      this.txtYear.Clear();
      this.txtDescription.Clear();
      this.txtPricePerHour.Clear();
      this.txtBikeOnRental.Clear();
      this.txtBikeRentalInfo.Clear();
    }


    //
    // List all the bikes:
    //
    private void cmdLoadBikes_Click(object sender, EventArgs e)
    {
      this.lstBikes.Items.Clear();
      ClearBikeInfo();

            // ADDED INDEX BikeTypes_PricePerHour FOR FASTER EXECUTION

            string sql = string.Format(@"
Select BID, Year, Description, PricePerHour
From Bikes
Inner Join BikeTypes WITH (INDEX(BikeTypes_PricePerHour)) On Bikes.TID = BikeTypes.TID
Order by BID;
");

      //MessageBox.Show(sql);

      DataAccessTier.Data datatier = new DataAccessTier.Data(dbfilename);

      try
      {
        DataSet ds = datatier.ExecuteNonScalarQuery(sql);

        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          Bike b = new Bike(
            Convert.ToInt32(row["BID"]),
            Convert.ToInt32(row["Year"]),
            row["Description"].ToString(),
            Convert.ToDecimal(row["PricePerHour"]));

          this.lstBikes.Items.Add(b);
        }

      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }


    //
    // User has clicked on a bike in the list box, so display information 
    // about this bike:
    //
    private void lstBikes_SelectedIndexChanged(object sender, EventArgs e)
    {
      ClearBikeInfo();

      if (this.lstBikes.SelectedIndex < 0)  // nothing selected:
        return;

      //
      // retrieve selected bike object from list box:
      //
      Bike b = (Bike)this.lstBikes.SelectedItem;

      this.txtYear.Text = b.Year.ToString();
      this.txtDescription.Text = b.Description;
      this.txtPricePerHour.Text = string.Format("${0:0.00}", b.PricePerHour);

      //
      // is bike rented?  If so, also display when it's due back...
      //
      DateTime dueback;

      if (b.OnRental(out dueback))
      {
        this.txtBikeOnRental.Text = string.Format("bike is rented");
        this.txtBikeRentalInfo.Text = string.Format("due: {0}", dueback);
      }
      else
      {
        this.txtBikeOnRental.Text = string.Format("bike *not* rented");
      }
    }


    //
    // Lists all the bikes available for rent.
    //
    private void cmdForRent_Click(object sender, EventArgs e)
    {
      this.lstForRent.Items.Clear();

            // ADDED INDEX BikeTypes_PricePerHour FOR FASTER EXECUTION

      string sql = string.Format(@"
Select BID, Description
From Bikes
Inner Join BikeTypes WITH (INDEX(BikeTypes_PricePerHour)) On Bikes.TID = BikeTypes.TID          
Where Rented = 0
Order by Description ASC, Year DESC, BID ASC;
");

      //MessageBox.Show(sql);

      DataAccessTier.Data datatier = new DataAccessTier.Data(dbfilename);

      try
      {
        DataSet ds = datatier.ExecuteNonScalarQuery(sql);

        string prevDesc = "";
        string curDesc = "";

        foreach (DataRow row in ds.Tables["TABLE"].Rows)
        {
          curDesc = row["Description"].ToString();

          if (curDesc != prevDesc)  // start a new section:
          {
            this.lstForRent.Items.Add(curDesc);
            prevDesc = curDesc;
          }

          string msg = string.Format("  {0}", row["BID"]);

          this.lstForRent.Items.Add(msg);
        }

      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
      }
    }


    //
    // User has clicked "rental" button for selected customer to rent
    // the selected bikes.
    //
    private void cmdRental_Click(object sender, EventArgs e)
    {
            string version = "MSSQLLocalDB";  

            string DBFile = "BikeHike.mdf";
            string DBConnectionInfo = String.Format(@"Data Source=(LocalDB)\{0};AttachDbFilename=|DataDirectory|\{1};Integrated Security=True;",
              version,
              DBFile);

            // Set up connection
            SqlConnection db;
            db = new SqlConnection(DBConnectionInfo);
            db.Open();

            // Initialize transaction (serializable)
            SqlTransaction tx = db.BeginTransaction(IsolationLevel.Serializable);

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = db;


            // is a customer selected?  are they already renting?
            //
            Customer c = (Customer)this.lstCustomers.SelectedItem;

            if (c == null)
            {
                MessageBox.Show("Please select a customer...");
                return;
            }

            if (c.OnRental())  // already renting, cannot proceed:
            {
                MessageBox.Show("Sorry, this customer is already renting one or more bikes...");
                tx.Rollback();
                db.Close();
                return;
            }

            //
            // what bikes do they want to rent?  Loop through listbox and collected the
            // checked items in a list:
            //
            List<int> bids = new List<int>();

            foreach (string s in lstForRent.CheckedItems)
            {
                // did user check description by chance?
                int bid;

                if (Int32.TryParse(s, out bid))  // valid bike id is selected:
                {
                        bids.Add(bid);
                }
                else
                    ; // ignore, not a bike id:
            }
 

            //
            // how many do we have?  Rent them if N > 0...
            //
            if (bids.Count == 0)
            {
                MessageBox.Show("Please select at least one bicycle to rent...");
                return;
            }

            //
            // Okay, we have N>0 bikes to rent in our list, how long is the rental?
            //
            int N = bids.Count;

            double duration;

            if (!Double.TryParse(this.txtDuration.Text, out duration))
            {
                MessageBox.Show("Please enter a valid duration in # of hours (e.g. 3.5)...");
                return;
            }

            // Delay
            int timeInMS;

            if (System.Int32.TryParse(this.textBox1.Text, out timeInMS) == true)
                ;
            else
                timeInMS = 0;    //  no  delay:

            System.Threading.Thread.Sleep(timeInMS);

            // Used as check for deadlock
            int retries = 0;

            while (retries < 3)
            {
                try
                {
                    int RID = 0;

                    string sql = string.Format(@"
INSERT INTO
Rentals(CID, StartTime, ExpDuration, NumBikes)
VALUES ({0}, GetDate(), {1}, {2});

DECLARE @rid AS INT;
SET @rid = @@IDENTITY;
SELECT @rid AS RID;
",
              c.CID, duration, N);

                    cmd.CommandText = sql;
                    cmd.Transaction = tx;

                    object result = cmd.ExecuteScalar();

                    // Rollback
                    if (result == null)
                    {
                        tx.Rollback();
                        break;
                    }

                    RID = Convert.ToInt32(result);

                    //
                    // traverse the list of bikes, building up an insert and
                    // update query:
                    //
                    string insertSQL = "";
                    string updateSQL = "";

                    bool first = true;

                    foreach (int bid in bids)
                    {
                        // Check if bikes are already rented

                        string SQL = string.Format(@"Select Rented From Bikes Where BID = {0}", bid);
                        cmd.CommandText = SQL;
                        cmd.Transaction = tx;

                        object r = cmd.ExecuteScalar();

                        // Rollback
                        if (r.ToString() == "True")
                        {
                            MessageBox.Show("Already rented");
                            tx.Rollback();
                            return;
                        }

                        if (first)  //create base query:
                        {
                            first = false;

                            insertSQL = string.Format(@"
INSERT INTO
RentalDetails(RID, BID)
VALUES ({0}, {1})
", RID, bid);

                            updateSQL = string.Format(@"
UPDATE Bikes
SET Rented = 1
WHERE BID = {0}
", bid);

                        }
                        else  // append to base query for next insert / update:
                        {

                            insertSQL = string.Format(@"
{0}, ({1}, {2})
", insertSQL, RID, bid);


                            updateSQL = string.Format(@"
{0} OR BID = {1}
", updateSQL, bid);

                        }

                    }//foreach


                    //
                    // Execute the inserts & updates:
                    //
                    sql = insertSQL + ";" + updateSQL + ";";

                    cmd.CommandText = sql;
                    cmd.Transaction = tx;

                    object result2 = cmd.ExecuteNonQuery();

                    // Rollback
                    if (Convert.ToInt32(result2) != N * 2)
                    {
                        tx.Rollback();
                        break;
                    }
                    else // success:
                    {
                        string msg;

                        if (N == 1)
                            msg = string.Format("Successful rental of 1 bike (RID={0})", RID);
                        else
                            msg = string.Format("Successful rental of {0} bikes (RID={1})", N, RID);

                        MessageBox.Show(msg);

                        tx.Commit();
                    }

                    //
                    // Success, update GUI since visible data may have changed:
                    //

                    // Update selected customer since now on a rental:
                    int index = this.lstCustomers.SelectedIndex;
                    if (index >= 0)
                    {
                        this.lstCustomers.SelectedIndex = -1;
                        this.lstCustomers.SelectedIndex = index;
                    }

                    // Likewise, update selected bike (if any):
                    index = this.lstBikes.SelectedIndex;
                    if (index >= 0)
                    {
                        this.lstBikes.SelectedIndex = -1;
                        this.lstBikes.SelectedIndex = index;
                    }



                    // Finally, update the list of available bikes:
                    cmdForRent_Click(sender, e);

                    break;
                }
                // Deadlock
                catch (SqlException ex)
                {
                    if (ex.Number == 1205)
                    {
                        retries++;
                    }
                    else
                    {
                        break;
                    }
                }
                // Rollback
                catch (Exception ex)
                {
                    tx.Rollback();
                    MessageBox.Show(ex.Message);
                    break;
                }
                finally
                {
                    db.Close();
                }
            }
    }


        //
        // User has clicked button indicating that selected customer is returning 
        // from their rental.
        //
        private void cmdReturn_Click(object sender, EventArgs e)
        {
            string version = "MSSQLLocalDB";  // for VS 2015 and newer:

            string DBFile = "BikeHike.mdf";
            string DBConnectionInfo = String.Format(@"Data Source=(LocalDB)\{0};AttachDbFilename=|DataDirectory|\{1};Integrated Security=True;",
              version,
              DBFile);

            // Set up connection
            SqlConnection db;
            db = new SqlConnection(DBConnectionInfo);
            db.Open();

            // Initialize transaction (serializable)
            SqlTransaction tx = db.BeginTransaction(IsolationLevel.Serializable);

            // is a customer selected?
            Customer c = (Customer)this.lstCustomers.SelectedItem;

            if (c == null)
            {
                MessageBox.Show("Please select a customer...");
                return;
            }

            int RID, N;
            DateTime start;
            double expectedDuration;

            if (!c.OnRental(out RID, out start, out expectedDuration, out N))
            {
                MessageBox.Show("This customer has no bikes to return...");
                tx.Rollback();
                db.Close();
                return;
            }

            //
            // Compute duration of rental:
            //
            TimeSpan duration = DateTime.Now.Subtract(start);

            double hours = (duration.Days * 24) +
              duration.Hours +
              (duration.Minutes / 60.0) +
              (duration.Seconds / 3600.00);

            // Used for deadlock
            int retries = 0;

            //MessageBox.Show("duration: " + hours.ToString());

            while(retries < 3) { 
            //
            // now lookup the bikes involved in rental, and compute
            // total price:
            //
            try
            {

                // ADDED INDEX BikeTypes_PricePerHour FOR FASTER EXECUTION

                string sql = string.Format(@"
SELECT Bikes.BID, PricePerHour
FROM RentalDetails
INNER JOIN Bikes ON RentalDetails.BID = Bikes.BID
INNER JOIN BikeTypes WITH (INDEX(BikeTypes_PricePerHour)) ON Bikes.TID = BikeTypes.TID
WHERE RID = {0};
", RID);

                //MessageBox.Show(sql);

                DataAccessTier.Data datatier = new DataAccessTier.Data(dbfilename);

                double totalprice = 0.0;

                List<int> bids = new List<int>();

                DataSet ds = datatier.ExecuteNonScalarQuery(sql);

                foreach (DataRow row in ds.Tables["Table"].Rows)
                {
                    bids.Add(Convert.ToInt32(row["BID"]));
                    totalprice += (hours * Convert.ToDouble(row["PricePerHour"]));
                }

                //
                // we have what we need to update the rental record:
                //
                sql = string.Format(@"
UPDATE Rentals
  SET ActDuration = {0}, TotalPrice = {1}
  WHERE RID = {2};
", hours, totalprice, RID);

                //MessageBox.Show(sql);

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = db;
                cmd.CommandText = sql;
                cmd.Transaction = tx;

                object result = cmd.ExecuteNonQuery();

                // Rollback
                if (System.Int32.Parse(result.ToString()) != 1)
                {
                    tx.Rollback();
                    break;
                }

                //
                // Finally, update each of the bikes' Rented flag so 
                // they can be rented again:
                //
                bool first = true;

                sql = "";

                foreach (int bid in bids)
                {
                    if (first)  //create base query:
                    {
                        first = false;

                        sql = string.Format(@"
UPDATE Bikes
SET Rented = 0
WHERE BID = {0}
", bid);
                    }
                    else  // append to base query for next update:
                    {
                        sql = string.Format(@"
{0} OR BID = {1}
", sql, bid);
                    }

                }//foreach

                sql = sql + ";";

                cmd.CommandText = sql;
                cmd.Transaction = tx;

                object result2 = cmd.ExecuteNonQuery();
                
                // Delay
                int timeInMS;

                if (System.Int32.TryParse(this.textBox1.Text, out timeInMS) == true)
                    ;
                else
                    timeInMS = 0;    //  no  delay:

                System.Threading.Thread.Sleep(timeInMS);
                
                // Rollback
                if (bids.Count != N)  // database correctness check:
                {
                    MessageBox.Show("**Sanity check: # of bikes in rentals table doesn't agree with # of bikes in details table?!");
                    tx.Rollback();
                    break;
                }

                // Rollback
                if (System.Int32.Parse(result2.ToString()) != N)
                {
                    tx.Rollback();
                    break;
                }

                //
                // Success, rental updated and all bikes returned:
                //
                string msg = string.Format("Return complete, total cost ${0:0.00}", totalprice);

                MessageBox.Show(msg);

                tx.Commit();

                //
                // Update selected customer since no longer renting:
                //
                int index = this.lstCustomers.SelectedIndex;
                if (index >= 0)
                {
                    this.lstCustomers.SelectedIndex = -1;
                    this.lstCustomers.SelectedIndex = index;
                }

                //
                // Likewise, update selected bike (if any):
                //
                index = this.lstBikes.SelectedIndex;
                if (index >= 0)
                {
                    this.lstBikes.SelectedIndex = -1;
                    this.lstBikes.SelectedIndex = index;
                }

                //
                // Finally, update the list of available bikes:
                //
                cmdForRent_Click(sender, e);

                break;
            }
            // Deadlock
            catch (SqlException ex)
            {
                if (ex.Number == 1205)
                {
                    retries++;
                }
                else
                {
                    break;
                }
            }
            // Rollback
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show(ex.Message);
                break;
            }
            finally
            {
                db.Close();
            }
        }
  }


    //
    // Exit the app by closing main window.
    //
    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      this.Close();
    }


    //
    // Reset the database by deleting all rental records, and resetting all bikes
    // as available for rent.  Great for testing.
    //
    private void resetDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
    {
            string version = "MSSQLLocalDB";  // for VS 2015 and newer:

            string DBFile = "BikeHike.mdf";
            string DBConnectionInfo = String.Format(@"Data Source=(LocalDB)\{0};AttachDbFilename=|DataDirectory|\{1};Integrated Security=True;",
              version,
              DBFile);

            // Set up connection
            SqlConnection db;
            db = new SqlConnection(DBConnectionInfo);
            db.Open();

            // Initialize transaction (serializable)
            SqlTransaction tx = db.BeginTransaction(IsolationLevel.Serializable);

            // Used for deadlock
            int retries = 0;

            while (retries < 3)
            {
                //
                // Reset the database, i.e. delete all the rentals and
                // set all bikes to *not* rented:
                //
                try
                {
                    string sql = string.Format(@"
DELETE FROM RentalDetails;  -- delete all rental details:

DELETE FROM Rentals;  -- now delete all rentals:

UPDATE Bikes SET Rented = 0;
");

                    //MessageBox.Show(sql);

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = db;
                    cmd.CommandText = sql;
                    cmd.Transaction = tx;

                    object result = cmd.ExecuteScalar();

                    tx.Commit();
                    break;
                }
                // Deadlock
                catch (SqlException ex)
                {
                    if (ex.Number == 1205)
                    {
                        retries++;
                    }
                    else
                    {
                        break;
                    }

                }
                // Rollback
                catch (Exception ex)
                {
                    tx.Rollback();
                    MessageBox.Show(ex.Message);
                    break;
                }
                finally
                {
                    db.Close();
                }
            }

      //
      // reset the GUI:
      //
      cmdLoadCustomers_Click(sender, e);
      cmdForRent_Click(sender, e);
      cmdLoadBikes_Click(sender, e);
    }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //int seconds = System.Int32.Parse(this.textBox1.Text);
        }
    }//class
}//namespace
