using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Common;

namespace LostFound.DAL
{
    public static class clsClaimData
    {
        public static async Task<int?> AddNewClaim(string Proof, string? PhotoPath, int UserID, int ReportID)
        {
            int? NewRecordID = null;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "insert into Claims (Status, Proof, PhotoPath, UserID, ReportID) values (1, @Proof, @PhotoPath, @UserID, @ReportID); select scope_identity();";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Proof", SqlDbType.NVarChar).Value = Proof ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@PhotoPath", SqlDbType.NVarChar).Value = PhotoPath ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
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

        public static async Task<DataTable> GetClaimByID(int ClaimID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ClaimID as [ClaimID], CreatedDate as [CreatedDate], Status as [Status], Proof as [Proof], PhotoPath as [PhotoPath], UserID as [UserID], ReportID as [ReportID], AdminID as [AdminID], RejectionReason as [RejectionReason] from Claims where ClaimID = @ClaimID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
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

        public static async Task<DataTable> GetAllClaims(int? Status)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ClaimID as [ClaimID], CreatedDate as [CreatedDate], Status as [Status], Proof as [Proof], PhotoPath as [PhotoPath], UserID as [UserID], ReportID as [ReportID], AdminID as [AdminID], RejectionReason as [RejectionReason] from Claims where (@Status is null or Status = @Status) order by ClaimID desc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Status", SqlDbType.Int).Value = Status ?? (object)DBNull.Value;
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

        public static async Task<DataTable> GetClaimsByUserID(int UserID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ClaimID as [ClaimID], CreatedDate as [CreatedDate], Status as [Status], Proof as [Proof], PhotoPath as [PhotoPath], UserID as [UserID], ReportID as [ReportID], AdminID as [AdminID], RejectionReason as [RejectionReason] from Claims where UserID = @UserID order by ClaimID desc";
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

        public static async Task<DataTable> GetClaimsByReportID(int ReportID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ClaimID as [ClaimID], CreatedDate as [CreatedDate], Status as [Status], Proof as [Proof], PhotoPath as [PhotoPath], UserID as [UserID], ReportID as [ReportID], AdminID as [AdminID], RejectionReason as [RejectionReason] from Claims where ReportID = @ReportID order by ClaimID desc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
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

        public static async Task<bool> ApproveClaim(int ClaimID, int AdminID)
        {
            bool IsApproved = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    await conn.OpenAsync();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int? ReportID = null;
                            string getReportQuery = "select ReportID from Claims where ClaimID = @ClaimID and Status = 1";
                            using (SqlCommand getCmd = new SqlCommand(getReportQuery, conn, transaction))
                            {
                                getCmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                                object result = await getCmd.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                    ReportID = Convert.ToInt32(result);
                            }

                            if (!ReportID.HasValue)
                            {
                                await transaction.RollbackAsync();
                                return false;
                            }

                            string approveQuery = "update Claims set Status = 2, AdminID = @AdminID, RejectionReason = null where ClaimID = @ClaimID and Status = 1";
                            using (SqlCommand approveCmd = new SqlCommand(approveQuery, conn, transaction))
                            {
                                approveCmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                                approveCmd.Parameters.Add("@AdminID", SqlDbType.Int).Value = AdminID;
                                IsApproved = (await approveCmd.ExecuteNonQueryAsync() > 0);
                            }

                            if (IsApproved)
                            {
                                string rejectOthersQuery = "update Claims set Status = 3, AdminID = @AdminID, RejectionReason = isnull(RejectionReason, 'Another claim on this report was approved') where ReportID = @ReportID and ClaimID <> @ClaimID and Status = 1";
                                using (SqlCommand rejectCmd = new SqlCommand(rejectOthersQuery, conn, transaction))
                                {
                                    rejectCmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID.Value;
                                    rejectCmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                                    rejectCmd.Parameters.Add("@AdminID", SqlDbType.Int).Value = AdminID;
                                    await rejectCmd.ExecuteNonQueryAsync();
                                }

                                string updateReportQuery = "update Reports set Status = 2 where ReportID = @ReportID";
                                using (SqlCommand reportCmd = new SqlCommand(updateReportQuery, conn, transaction))
                                {
                                    reportCmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID.Value;
                                    await reportCmd.ExecuteNonQueryAsync();
                                }
                            }

                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsApproved = false;
            }
            return IsApproved;
        }

        public static async Task<bool> RejectClaim(int ClaimID, int AdminID, string RejectionReason)
        {
            bool IsRejected = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Claims set Status = 3, AdminID = @AdminID, RejectionReason = @RejectionReason where ClaimID = @ClaimID and Status = 1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                        cmd.Parameters.Add("@AdminID", SqlDbType.Int).Value = AdminID;
                        cmd.Parameters.Add("@RejectionReason", SqlDbType.NVarChar).Value = RejectionReason ?? (object)DBNull.Value;
                        await conn.OpenAsync();
                        IsRejected = (await cmd.ExecuteNonQueryAsync() > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsRejected = false;
            }
            return IsRejected;
        }

        public static async Task<bool> CancelClaim(int ClaimID, int UserID)
        {
            bool IsCancelled = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Claims set Status = 4 where ClaimID = @ClaimID and UserID = @UserID and Status = 1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
                        await conn.OpenAsync();
                        IsCancelled = (await cmd.ExecuteNonQueryAsync() > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsCancelled = false;
            }
            return IsCancelled;
        }

        public static async Task<bool> ReopenClaim(int ClaimID, int AdminID)
        {
            bool IsReopened = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = @"update c
set c.Status = 1, c.AdminID = @AdminID, c.RejectionReason = null
from Claims c
inner join Reports r on r.ReportID = c.ReportID
where c.ClaimID = @ClaimID
  and c.Status = 3
  and r.Status = 1
  and not exists (
      select 1 from Claims other
      where other.ReportID = c.ReportID
        and other.ClaimID <> c.ClaimID
        and other.Status in (1, 2)
  )";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = ClaimID;
                        cmd.Parameters.Add("@AdminID", SqlDbType.Int).Value = AdminID;
                        await conn.OpenAsync();
                        IsReopened = (await cmd.ExecuteNonQueryAsync() > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
                IsReopened = false;
            }
            return IsReopened;
        }
    }
}
