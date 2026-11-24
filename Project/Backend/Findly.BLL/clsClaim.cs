using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Findly.BLL.Enums;
using LostFound.DAL;

namespace LostFound.BLL
{
    public partial class clsClaim
    {
        private enum enMode { AddNew, Edit }
        private enMode _Mode;

        public int ClaimID { get; set; }
        public DateTime CreatedDate { get; set; }
        public enClaimStatus Status { get; set; }
        public string Proof { get; set; }
        public string? PhotoPath { get; set; }
        public int UserID { get; set; }
        public int ReportID { get; set; }
        public int? AdminID { get; set; }
        public string? RejectionReason { get; set; }

        public clsClaim()
        {
            this._Mode = enMode.AddNew;
            this.ClaimID = 0;
            this.CreatedDate = DateTime.Now;
            this.Status = enClaimStatus.Pending;
            this.Proof = string.Empty;
            this.PhotoPath = null;
            this.UserID = 0;
            this.ReportID = 0;
            this.AdminID = null;
            this.RejectionReason = null;
        }

        private clsClaim(int claimid, DateTime createddate, enClaimStatus status, string proof, string? PhotoPath, int userid, int reportid, int? adminid, string? rejectionreason)
        {
            this._Mode = enMode.Edit;
            this.ClaimID = claimid;
            this.CreatedDate = createddate;
            this.Status = status;
            this.Proof = proof;
            this.PhotoPath = PhotoPath;
            this.UserID = userid;
            this.ReportID = reportid;
            this.AdminID = adminid;
            this.RejectionReason = rejectionreason;
        }

        private static clsClaim _MapRowToClaim(DataRow row)
        {
            int claimid = Convert.ToInt32(row["ClaimID"]);
            DateTime createddate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.MinValue;
            enClaimStatus status = row["Status"] != DBNull.Value ? (enClaimStatus)Convert.ToInt32(row["Status"]) : enClaimStatus.Pending;
            string proof = row["Proof"] != DBNull.Value ? Convert.ToString(row["Proof"])! : string.Empty;
            string? PhotoPath = row["PhotoPath"] != DBNull.Value ? Convert.ToString(row["PhotoPath"]) : null;
            int userid = row["UserID"] != DBNull.Value ? Convert.ToInt32(row["UserID"]) : 0;
            int reportid = row["ReportID"] != DBNull.Value ? Convert.ToInt32(row["ReportID"]) : 0;
            int? adminid = row["AdminID"] != DBNull.Value ? Convert.ToInt32(row["AdminID"]) : null;
            string? rejectionreason = row["RejectionReason"] != DBNull.Value ? Convert.ToString(row["RejectionReason"]) : null;

            return new clsClaim(claimid, createddate, status, proof, PhotoPath, userid, reportid, adminid, rejectionreason);
        }

        public async Task<bool> Create()
        {
            clsReport? report = await clsReport.Find(this.ReportID);
            if (report == null)
                return false;

            if (report.Type != enReportType.Found || report.Status != enReportStatus.Open)
                return false;

            DataTable dtExistingClaims = await clsClaimData.GetClaimsByReportID(this.ReportID);
            foreach (DataRow row in dtExistingClaims.Rows)
            {
                int claimUserID = row["UserID"] != DBNull.Value ? Convert.ToInt32(row["UserID"]) : 0;
                enClaimStatus claimStatus = row["Status"] != DBNull.Value ? (enClaimStatus)Convert.ToInt32(row["Status"]) : enClaimStatus.Pending;

                if (claimUserID == this.UserID)
                {
                    if (claimStatus == enClaimStatus.Pending || claimStatus == enClaimStatus.Approved || claimStatus == enClaimStatus.Rejected)
                        return false;
                }
            }

            this._Mode = enMode.AddNew;
            this.Status = enClaimStatus.Pending;

            int? NewID = await clsClaimData.AddNewClaim(this.Proof, this.PhotoPath, this.UserID, this.ReportID);
            if (NewID.HasValue)
            {
                this.ClaimID = NewID.Value;
                this._Mode = enMode.Edit;
                return true;
            }
            else
                return false;
        }

        public static async Task<clsClaim?> Find(int claimID)
        {
            DataTable dtRecords = await clsClaimData.GetClaimByID(claimID);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToClaim(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<List<clsClaim>> GetAll(enClaimStatus? status)
        {
            List<clsClaim> lstClaims = new List<clsClaim>();
            DataTable dtRecords = await clsClaimData.GetAllClaims(status.HasValue ? (int?)status.Value : null);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstClaims.Add(_MapRowToClaim(row));
            }
            return lstClaims;
        }

        public static async Task<List<clsClaim>> GetByUserID(int userID)
        {
            List<clsClaim> lstClaims = new List<clsClaim>();
            DataTable dtRecords = await clsClaimData.GetClaimsByUserID(userID);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstClaims.Add(_MapRowToClaim(row));
            }
            return lstClaims;
        }

        public static async Task<List<clsClaim>> GetByReportID(int reportID)
        {
            List<clsClaim> lstClaims = new List<clsClaim>();
            DataTable dtRecords = await clsClaimData.GetClaimsByReportID(reportID);
            foreach (DataRow row in dtRecords.Rows)
            {
                lstClaims.Add(_MapRowToClaim(row));
            }
            return lstClaims;
        }

        public async Task<bool> Approve(int adminID)
        {
            if (this.Status != enClaimStatus.Pending)
                return false;

            bool IsApproved = await clsClaimData.ApproveClaim(this.ClaimID, adminID);
            if (IsApproved)
            {
                this.Status = enClaimStatus.Approved;
                this.AdminID = adminID;
                this.RejectionReason = null;
            }
            return IsApproved;
        }

        public async Task<bool> Reject(int adminID, string rejectionReason)
        {
            if (this.Status != enClaimStatus.Pending)
                return false;

            bool IsRejected = await clsClaimData.RejectClaim(this.ClaimID, adminID, rejectionReason);
            if (IsRejected)
            {
                this.Status = enClaimStatus.Rejected;
                this.AdminID = adminID;
                this.RejectionReason = rejectionReason;
            }
            return IsRejected;
        }

        public async Task<bool> Cancel(int currentUserID)
        {
            if (this.UserID != currentUserID || this.Status != enClaimStatus.Pending)
                return false;

            bool IsCancelled = await clsClaimData.CancelClaim(this.ClaimID, currentUserID);
            if (IsCancelled)
                this.Status = enClaimStatus.Cancelled;

            return IsCancelled;
        }

        public async Task<bool> Reopen(int adminID)
        {
            if (this.Status != enClaimStatus.Rejected)
                return false;

            bool IsReopened = await clsClaimData.ReopenClaim(this.ClaimID, adminID);
            if (IsReopened)
            {
                this.Status = enClaimStatus.Pending;
                this.AdminID = adminID;
                this.RejectionReason = null;
            }
            return IsReopened;
        }
    }
}
