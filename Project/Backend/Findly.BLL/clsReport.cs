using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Findly.BLL.Enums;
using LostFound.DAL;

namespace LostFound.BLL
{
    public partial class clsReport
    {
        private enum enMode { AddNew, Edit }
        private enMode _Mode;

        public int ReportID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ReportDate { get; set; }
        public enReportType Type { get; set; }
        public enReportStatus Status { get; set; }
        public bool HasPhoto { get; set; }
        public int UserID { get; set; }
        public int CategoryID { get; set; }
        public int LocationID { get; set; }

        public clsReport()
        {
            this._Mode = enMode.AddNew;
            this.ReportID = 0;
            this.Title = string.Empty;
            this.Description = string.Empty;
            this.ReportDate = DateTime.Now;
            this.Type = enReportType.Lost;
            this.Status = enReportStatus.Open;
            this.HasPhoto = false;
            this.UserID = 0;
            this.CategoryID = 0;
            this.LocationID = 0;
        }

        private clsReport(int reportid, string title, string description, DateTime reportdate, enReportType type, enReportStatus status, bool hasphoto, int userid, int categoryid, int locationid)
        {
            this._Mode = enMode.Edit;
            this.ReportID = reportid;
            this.Title = title;
            this.Description = description;
            this.ReportDate = reportdate;
            this.Type = type;
            this.Status = status;
            this.HasPhoto = hasphoto;
            this.UserID = userid;
            this.CategoryID = categoryid;
            this.LocationID = locationid;
        }

        private static clsReport _MapRowToReport(DataRow row)
        {
            int reportid = Convert.ToInt32(row["ReportID"]);
            string title = row["Title"] != DBNull.Value ? Convert.ToString(row["Title"])! : string.Empty;
            string description = row["Description"] != DBNull.Value ? Convert.ToString(row["Description"])! : string.Empty;
            DateTime reportdate = row["ReportDate"] != DBNull.Value ? Convert.ToDateTime(row["ReportDate"]) : DateTime.MinValue;
            enReportType type = row["Type"] != DBNull.Value ? (enReportType)Convert.ToInt32(row["Type"]) : enReportType.Lost;
            enReportStatus status = row["Status"] != DBNull.Value ? (enReportStatus)Convert.ToInt32(row["Status"]) : enReportStatus.Open;
            bool hasphoto = row["HasPhoto"] != DBNull.Value && Convert.ToBoolean(row["HasPhoto"]);
            int userid = row["UserID"] != DBNull.Value ? Convert.ToInt32(row["UserID"]) : 0;
            int categoryid = row["CategoryID"] != DBNull.Value ? Convert.ToInt32(row["CategoryID"]) : 0;
            int locationid = row["LocationID"] != DBNull.Value ? Convert.ToInt32(row["LocationID"]) : 0;

            return new clsReport(reportid, title, description, reportdate, type, status, hasphoto, userid, categoryid, locationid);
        }

        private async Task<bool> _AddNew()
        {
            this.Status = enReportStatus.Open;
            int? NewID = await clsReportData.AddNewReport(this.Title, this.Description, this.ReportDate, (int)this.Type, this.UserID, this.CategoryID, this.LocationID);
            if (NewID.HasValue)
            {
                this.ReportID = NewID.Value;
                return true;
            }
            else
                return false;
        }

        private Task<bool> _Edit()
        {
            return clsReportData.UpdateReport(this.ReportID, this.Title, this.Description, this.ReportDate, this.CategoryID, this.LocationID);
        }

        public async Task<bool> Save()
        {
            switch (this._Mode)
            {
                case enMode.AddNew:
                    if (await this._AddNew())
                    {
                        this._Mode = enMode.Edit;
                        return true;
                    }
                    else
                        return false;
                case enMode.Edit:
                    return await this._Edit();
                default:
                    return false;
            }
        }

        public static async Task<clsReport?> Find(int reportID)
        {
            DataTable dtRecords = await clsReportData.GetReportByID(reportID);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToReport(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<List<clsReport>> GetAll(enReportType? type, enReportStatus? status, int? categoryID, int? locationID, string? searchText, int pageNumber, int pageSize)
        {
            List<clsReport> lstReports = new List<clsReport>();
            DataTable dtRecords = await clsReportData.GetAllReports(
                type.HasValue ? (int?)type.Value : null,
                status.HasValue ? (int?)status.Value : null,
                categoryID,
                locationID,
                searchText,
                pageNumber,
                pageSize
            );
            foreach (DataRow row in dtRecords.Rows)
            {
                lstReports.Add(_MapRowToReport(row));
            }
            return lstReports;
        }

        public static async Task<List<clsReport>> GetByUserID(int userID)
        {
            List<clsReport> lstReports = new List<clsReport>();
            DataTable dtRecords = await clsReportData.GetReportsByUserID(userID);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstReports.Add(_MapRowToReport(row));
            }
            return lstReports;
        }

        public async Task<bool> SetPhoto(byte[] photo, string contentType)
        {
            bool IsSaved = await clsReportData.SetReportPhoto(this.ReportID, photo, contentType);
            if (IsSaved)
                this.HasPhoto = true;
            return IsSaved;
        }

        public async Task<bool> RemovePhoto()
        {
            bool IsRemoved = await clsReportData.SetReportPhoto(this.ReportID, null, null);
            if (IsRemoved)
                this.HasPhoto = false;
            return IsRemoved;
        }

        public static Task<(byte[] Data, string ContentType)?> GetPhoto(int reportID)
        {
            return clsReportData.GetReportPhoto(reportID);
        }

        public Task<bool> Delete(int currentUserID, enUserRole currentUserRole)
        {
            bool IsAllowed = (currentUserRole == enUserRole.Admin) || (this.UserID == currentUserID);
            if (!IsAllowed)
                return Task.FromResult(false);

            return clsReportData.DeleteReport(this.ReportID);
        }

        public async Task<bool> ChangeStatus(enReportStatus newStatus, int currentUserID, enUserRole currentUserRole)
        {
            bool IsOwner = (this.UserID == currentUserID);
            bool IsAdmin = (currentUserRole == enUserRole.Admin);

            bool IsAllowed = false;
            if (IsAdmin)
            {
                IsAllowed = true;
            }
            else if (IsOwner && this.Status == enReportStatus.Open)
            {
                if (newStatus == enReportStatus.Closed)
                    IsAllowed = true;
                else if (newStatus == enReportStatus.Resolved && this.Type == enReportType.Lost)
                    IsAllowed = true;
            }

            if (!IsAllowed)
                return false;

            bool IsChanged = await clsReportData.ChangeReportStatus(this.ReportID, (int)newStatus);
            if (IsChanged)
                this.Status = newStatus;

            return IsChanged;
        }

        public async Task<List<clsReport>> GetMatches()
        {
            List<clsReport> lstMatches = new List<clsReport>();
            DataTable dtRecords = await clsReportData.GetMatchingReports(this.ReportID);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstMatches.Add(_MapRowToReport(row));
            }
            return lstMatches;
        }

        public static async Task<List<clsReport>> GetUnclaimed(int minimumAgeInDays = 30)
        {
            List<clsReport> lstUnclaimed = new List<clsReport>();
            DataTable dtRecords = await clsReportData.GetUnclaimedReports(minimumAgeInDays);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstUnclaimed.Add(_MapRowToReport(row));
            }
            return lstUnclaimed;
        }
    }
}
