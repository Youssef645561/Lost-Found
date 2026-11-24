using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Common;

namespace LostFound.DAL
{
    public static class clsCategoryData
    {
        public static async Task<int?> AddNewCategory(string Name)
        {
            int? NewRecordID = null;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "insert into Categories (Name) values (@Name); select scope_identity();";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = Name ?? (object)DBNull.Value;
                        await conn.OpenAsync();
                        object result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                            NewRecordID = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                NewRecordID = null;
            }
            return NewRecordID;
        }

        public static async Task<DataTable> GetCategoryByID(int CategoryID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select CategoryID as [CategoryID], Name as [Name] from Categories where CategoryID = @CategoryID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;
                        await conn.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                                dtRecords.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
            return dtRecords;
        }

        public static async Task<DataTable> GetAllCategories()
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select CategoryID as [CategoryID], Name as [Name] from Categories";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        await conn.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                                dtRecords.Load(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
            return dtRecords;
        }

        public static async Task<bool> UpdateCategory(int CategoryID, string Name)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Categories set Name = @Name where CategoryID = @CategoryID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;
                        cmd.Parameters.Add("@Name", SqlDbType.NVarChar).Value = Name ?? (object)DBNull.Value;
                        await conn.OpenAsync();
                        IsEdited = (await cmd.ExecuteNonQueryAsync() > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsEdited = false;
            }
            return IsEdited;
        }

        public static async Task<bool> DeleteCategory(int CategoryID)
        {
            bool IsDeleted = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "delete from Categories where CategoryID = @CategoryID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;
                        await conn.OpenAsync();
                        IsDeleted = (await cmd.ExecuteNonQueryAsync() > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsDeleted = false;
            }
            return IsDeleted;
        }
    }
}
