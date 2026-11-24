using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Common;

namespace LostFound.DAL
{
    public static class clsReportData
    {
        public static async Task<int?> AddNewReport(string Title, string Description, DateTime ReportDate, int Type, int UserID, int CategoryID, int LocationID)
        {
            int? NewRecordID = null;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "insert into Reports (Title, Description, ReportDate, Type, Status, UserID, CategoryID, LocationID) values (@Title, @Description, @ReportDate, @Type, 1, @UserID, @CategoryID, @LocationID); select scope_identity();";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar).Value = Title ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = Description ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@ReportDate", SqlDbType.DateTime).Value = ReportDate;
                        cmd.Parameters.Add("@Type", SqlDbType.Int).Value = Type;
                        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = UserID;
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID;
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

        public static async Task<DataTable> GetReportByID(int ReportID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ReportID as [ReportID], Title as [Title], Description as [Description], ReportDate as [ReportDate], Type as [Type], Status as [Status], convert(bit, case when Photo is null then 0 else 1 end) as [HasPhoto], UserID as [UserID], CategoryID as [CategoryID], LocationID as [LocationID], CreatedDate as [CreatedDate] from Reports where ReportID = @ReportID";
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

        public static async Task<DataTable> GetAllReports(int? Type, int? Status, int? CategoryID, int? LocationID, string? SearchText, int PageNumber, int PageSize)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = @"select ReportID as [ReportID], Title as [Title], Description as [Description], ReportDate as [ReportDate], Type as [Type], Status as [Status], convert(bit, case when Photo is null then 0 else 1 end) as [HasPhoto], UserID as [UserID], CategoryID as [CategoryID], LocationID as [LocationID], CreatedDate as [CreatedDate]
from Reports
where (@Type is null or Type = @Type)
  and (@Status is null or Status = @Status)
  and (@CategoryID is null or CategoryID = @CategoryID)
  and (@LocationID is null or LocationID = @LocationID)
  and (@SearchText is null or Title like @SearchText or Description like @SearchText)
order by ReportID desc
offset ((@PageNumber - 1) * @PageSize) rows fetch next @PageSize rows only";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@Type", SqlDbType.Int).Value = Type ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Status", SqlDbType.Int).Value = Status ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = !string.IsNullOrWhiteSpace(SearchText) ? $"%{SearchText}%" : (object)DBNull.Value;
                        cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = PageNumber;
                        cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = PageSize;
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

        public static async Task<DataTable> GetReportsByUserID(int UserID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select ReportID as [ReportID], Title as [Title], Description as [Description], ReportDate as [ReportDate], Type as [Type], Status as [Status], convert(bit, case when Photo is null then 0 else 1 end) as [HasPhoto], UserID as [UserID], CategoryID as [CategoryID], LocationID as [LocationID], CreatedDate as [CreatedDate] from Reports where UserID = @UserID order by ReportID desc";
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

        public static async Task<bool> UpdateReport(int ReportID, string Title, string Description, DateTime ReportDate, int CategoryID, int LocationID)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Reports set Title = @Title, Description = @Description, ReportDate = @ReportDate, CategoryID = @CategoryID, LocationID = @LocationID where ReportID = @ReportID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar).Value = Title ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = Description ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@ReportDate", SqlDbType.DateTime).Value = ReportDate;
                        cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = CategoryID;
                        cmd.Parameters.Add("@LocationID", SqlDbType.Int).Value = LocationID;
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

        public static async Task<bool> SetReportPhoto(int ReportID, byte[]? Photo, string? ContentType)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "update Reports set Photo = @Photo, PhotoContentType = @ContentType where ReportID = @ReportID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
                        cmd.Parameters.Add("@Photo", SqlDbType.VarBinary, -1).Value = Photo ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@ContentType", SqlDbType.NVarChar, 100).Value = ContentType ?? (object)DBNull.Value;
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

        public static async Task<(byte[] Data, string ContentType)?> GetReportPhoto(int ReportID)
        {
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "select Photo, PhotoContentType from Reports where ReportID = @ReportID and Photo is not null";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
                        await conn.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                byte[] data = (byte[])reader["Photo"];
                                string contentType = reader["PhotoContentType"] != DBNull.Value ? (string)reader["PhotoContentType"] : "application/octet-stream";
                                return (data, contentType);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                clsLogger.Log(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
            return null;
        }

        public static async Task<bool> DeleteReport(int ReportID)
        {
            bool IsDeleted = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = "delete from Reports where ReportID = @ReportID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
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

        public static async Task<bool> ChangeReportStatus(int ReportID, int NewStatus)
        {
            bool IsEdited = false;
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    await conn.OpenAsync();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string updateReportQuery = "update Reports set Status = @Status where ReportID = @ReportID";
                            using (SqlCommand cmd = new SqlCommand(updateReportQuery, conn, transaction))
                            {
                                cmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
                                cmd.Parameters.Add("@Status", SqlDbType.Int).Value = NewStatus;
                                IsEdited = (await cmd.ExecuteNonQueryAsync() > 0);
                            }

                            if (IsEdited && (NewStatus == 2 || NewStatus == 3))
                            {
                                string rejectClaimsQuery = "update Claims set Status = 3, RejectionReason = isnull(RejectionReason, 'Report closed or resolved') where ReportID = @ReportID and Status = 1";
                                using (SqlCommand rejectCmd = new SqlCommand(rejectClaimsQuery, conn, transaction))
                                {
                                    rejectCmd.Parameters.Add("@ReportID", SqlDbType.Int).Value = ReportID;
                                    await rejectCmd.ExecuteNonQueryAsync();
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
                IsEdited = false;
            }
            return IsEdited;
        }

        public static async Task<DataTable> GetMatchingReports(int ReportID)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = @"select r2.ReportID as [ReportID], r2.Title as [Title], r2.Description as [Description], r2.ReportDate as [ReportDate], r2.Type as [Type], r2.Status as [Status], convert(bit, case when r2.Photo is null then 0 else 1 end) as [HasPhoto], r2.UserID as [UserID], r2.CategoryID as [CategoryID], r2.LocationID as [LocationID], r2.CreatedDate as [CreatedDate]
from Reports r1
inner join Reports r2 on r2.ReportID <> r1.ReportID
where r1.ReportID = @ReportID
  and r2.Type <> r1.Type
  and r2.Status = 1
  and r2.CategoryID = r1.CategoryID
  and r2.LocationID = r1.LocationID
order by r2.ReportDate desc";
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

        public static async Task<DataTable> GetUnclaimedReports(int MinimumAgeInDays)
        {
            DataTable dtRecords = new DataTable();
            try
            {
                using (SqlConnection conn = clsDataAccess.GetConnection())
                {
                    string query = @"select r.ReportID as [ReportID], r.Title as [Title], r.Description as [Description], r.ReportDate as [ReportDate], r.Type as [Type], r.Status as [Status], convert(bit, case when r.Photo is null then 0 else 1 end) as [HasPhoto], r.UserID as [UserID], r.CategoryID as [CategoryID], r.LocationID as [LocationID], r.CreatedDate as [CreatedDate]
from Reports r
where r.Type = 2
  and datediff(day, r.ReportDate, getdate()) >= @MinimumAgeInDays
  and not exists (
      select 1 from Claims c where c.ReportID = r.ReportID and c.Status = 2
  )
order by r.ReportDate asc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.Add("@MinimumAgeInDays", SqlDbType.Int).Value = MinimumAgeInDays;
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
    }
}
