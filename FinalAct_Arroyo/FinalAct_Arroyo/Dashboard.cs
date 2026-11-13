using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;

namespace FinalAct_Arroyo
{
    public partial class Dashboard : Form
    {
        private int selectedProductID = 0;

        public Dashboard()
        {
            InitializeComponent();

            addBtn.Click += addBtn_Click;
            updateBtn.Click += updateBtn_Click;
            deleteBtn.Click += deleteBtn_Click;
            clearBtn.Click += clearBtn_Click;
            refreshBtn.Click += refreshBtn_Click;

            categoryCmb.DropDownStyle = ComboBoxStyle.DropDownList;
            viewlist.CellClick += viewlist_CellClick;

            LoadProducts();
            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();
                    string query = @"SELECT COUNT(*) AS TotalProducts, SUM(IIF(Quantity > ReorderLevel, 1, 0)) AS InStock, SUM(IIF(Quantity > 0 AND Quantity <= ReorderLevel, 1, 0 )) AS LowStock, SUM(IIF(Quantity = 0, 1, 0 )) AS OutofStock FROM Products";

                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalLbl.Text = reader["TotalProducts"] != DBNull.Value ? reader["TotalProducts"].ToString() : "0";
                            stockLbl.Text = reader["InStock"] != DBNull.Value ? reader["InStock"].ToString() : "0";
                            lowLbl.Text = reader["LowStock"] != DBNull.Value ? reader["LowStock"].ToString() : "0";
                            outLbl.Text = reader["OutofStock"] != DBNull.Value ? reader["OutofStock"].ToString() : "0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Statistic error: " + ex.Message);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();
                    string query = @"SELECT * FROM Products ORDER BY ProductID DESC";

                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(query, con))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        viewlist.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load products: " + ex.Message);
            }
        }

        private bool ValidateProduct()
        {
            if (string.IsNullOrEmpty(nameTxt.Text))
            {
                MessageBox.Show("Please enter a product name");
                nameTxt.Focus();
                return false;
            }
            if (categoryCmb.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a product Category");
                categoryCmb.Focus();
                return false;
            }

            decimal price;
            if (!decimal.TryParse(priceTxt.Text, out price) || price < 0)
            {
                MessageBox.Show("Please enter a valid product price");
                priceTxt.Focus();
                return false;
            }

            int quantity;
            if (!int.TryParse(quantityTxt.Text, out quantity) || quantity < 0)
            {
                MessageBox.Show("Please enter a valid quantity");
                quantityTxt.Focus();
                return false;
            }

            int reorder;
            if (!int.TryParse(reorderTxt.Text, out reorder) || reorder < 0)
            {
                MessageBox.Show("Please enter a valid Reorder Level");
                reorderTxt.Focus();
                return false;
            }
            return true;
        }

        private void addBtn_Click(object sender, EventArgs e)
        {
            if (!ValidateProduct())
                return;

            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();
                    string query = @"INSERT INTO Products (ProductName, Category, Description, UnitPrice, Quantity, ReorderLevel, CreatedAt) VALUES (?,?,?,?,?,?, Now())";

                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = nameTxt.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = categoryCmb.Text;
                        cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = descTxt.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = decimal.Parse(priceTxt.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(quantityTxt.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(reorderTxt.Text);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Product added successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearField();
                LoadProducts();
                LoadDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding product: " + ex.Message);
            }
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product row to update");
                return;
            }
            if (!ValidateProduct())
                return;

            try
            {
                using (OleDbConnection con = Database.GetConnection())
                {
                    con.Open();
                    string query = @"UPDATE Products SET ProductName=?, Category=?, Description=?, UnitPrice=?, Quantity=?, ReorderLevel=? WHERE ProductID=?";

                    using (OleDbCommand cmd = new OleDbCommand(query, con))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = nameTxt.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = categoryCmb.Text;
                        cmd.Parameters.Add("?", OleDbType.LongVarWChar).Value = descTxt.Text.Trim();
                        cmd.Parameters.Add("?", OleDbType.Currency).Value = decimal.Parse(priceTxt.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(quantityTxt.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = int.Parse(reorderTxt.Text);
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedProductID;
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Product updated successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearField();
                LoadDashboardStats();
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating product: " + ex.Message);
            }
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            if (selectedProductID == 0)
            {
                MessageBox.Show("Please select a product row to delete");
                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to delete this product?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    using (OleDbConnection con = Database.GetConnection())
                    {
                        con.Open();
                        string query = @"DELETE FROM Products WHERE ProductID = ?";

                        using (OleDbCommand cmd = new OleDbCommand(query, con))
                        {
                            cmd.Parameters.Add("?", OleDbType.Integer).Value = selectedProductID;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Product deleted successfully");
                    ClearField();
                    LoadProducts();
                    LoadDashboardStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting product: " + ex.Message);
                }
            }
        }

        private void viewlist_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = viewlist.Rows[e.RowIndex];
                selectedProductID = Convert.ToInt32(row.Cells["ProductID"].Value);
                nameTxt.Text = row.Cells["ProductName"].Value?.ToString() ?? "";
                categoryCmb.Text = row.Cells["Category"].Value?.ToString() ?? "";
                descTxt.Text = row.Cells["Description"].Value?.ToString() ?? "";
                priceTxt.Text = row.Cells["UnitPrice"].Value?.ToString() ?? "";
                quantityTxt.Text = row.Cells["Quantity"].Value?.ToString() ?? "";
                reorderTxt.Text = row.Cells["ReorderLevel"].Value?.ToString() ?? "";
            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            ClearField();
        }

        private void ClearField()
        {
            nameTxt.Clear();
            categoryCmb.SelectedIndex = -1;
            descTxt.Clear();
            priceTxt.Clear();
            quantityTxt.Clear();
            reorderTxt.Clear();
            selectedProductID = 0;
            viewlist.ClearSelection();
        }

        private void refreshBtn_Click(object sender, EventArgs e)
        {
            LoadProducts();
            LoadDashboardStats();
            ClearField();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to log out?", "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SignIn_Form signIn = new SignIn_Form();
                signIn.Show();
                this.Hide();
            }
        }
    }
}