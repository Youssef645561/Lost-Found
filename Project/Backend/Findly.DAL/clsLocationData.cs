using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Common;

namespace LostFound.DAL
{
    public static class clsLocationData
    {
        public static async Task<int?> AddNewLocation(string Name)
        {
            int? NewRecordID = null;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "insert into Locations (Name) values (@Name); select scope_identity();";
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

        public static async Task<DataTable> GetLocationByID(int LocationID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select LocationID as [LocationID], Name as [Name] from Locations where LocationID = @LocationID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID;
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

        public static async Task<DataTable> GetAllLocations()
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select LocationID as [LocationID], Name as [Name] from Locations";
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

        public static async Task<bool> UpdateLocation(int LocationID, string Name)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Locations set Name = @Name where LocationID = @LocationID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID;
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

        public static async Task<bool> DeleteLocation(int LocationID)
        {
            bool IsDeleted = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "delete from Locations where LocationID = @LocationID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID;
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
