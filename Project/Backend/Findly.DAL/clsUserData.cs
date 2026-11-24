using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Common;

namespace LostFound.DAL
{
    public static class clsUserData
    {
        public static async Task<int?> AddNewUser(string Username, string PasswordHash, string Email, string? Phone, int Role)
        {
            int? NewRecordID = null;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "insert into Users (Username, PasswordHash, Email, Phone, Role) values (@Username, @PasswordHash, @Email, @Phone, @Role); select scope_identity();";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Username", SqlDbType.NVarChar).Value = Username ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar).Value = PasswordHash ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = Email ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar).Value = Phone ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Role", SqlDbType.Int).Value = Role;
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

        public static async Task<DataTable> GetUserByID(int UserID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select UserID as [UserID], Username as [Username], PasswordHash as [PasswordHash], Email as [Email], Phone as [Phone], Role as [Role], CreatedDate as [CreatedDate] from Users where UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
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

        public static async Task<DataTable> GetUserByUsername(string Username)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select UserID as [UserID], Username as [Username], PasswordHash as [PasswordHash], Email as [Email], Phone as [Phone], Role as [Role], CreatedDate as [CreatedDate] from Users where Username = @Username";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Username", SqlDbType.NVarChar).Value = Username ?? (object)DBNull.Value;
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

        public static async Task<DataTable> GetUserByEmail(string Email)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select UserID as [UserID], Username as [Username], PasswordHash as [PasswordHash], Email as [Email], Phone as [Phone], Role as [Role], CreatedDate as [CreatedDate] from Users where Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = Email ?? (object)DBNull.Value;
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

        public static async Task<DataTable> GetAllUsers()
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select UserID as [UserID], Username as [Username], PasswordHash as [PasswordHash], Email as [Email], Phone as [Phone], Role as [Role], CreatedDate as [CreatedDate] from Users";
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

        public static async Task<bool> UpdateUser(int UserID, string Username, string Email, string? Phone, int Role)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Users set Username = @Username, Email = @Email, Phone = @Phone, Role = @Role where UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
                        cmd.Parameters.Add("@Username", SqlDbType.NVarChar).Value = Username ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = Email ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar).Value = Phone ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Role", SqlDbType.Int).Value = Role;
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

        public static async Task<bool> DeleteUser(int UserID)
        {
            bool IsDeleted = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "delete from Users where UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
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

        public static async Task<bool> ChangePassword(int UserID, string NewPasswordHash)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Users set PasswordHash = @PasswordHash where UserID = @UserID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
                        cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar).Value = NewPasswordHash ?? (object)DBNull.Value;
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
    }
}
