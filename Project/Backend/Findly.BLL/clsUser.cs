using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Findly.BLL.Enums;
using LostFound.DAL;
using Common;

namespace LostFound.BLL
{
    public partial class clsUser
    {
        private enum enMode { AddNew, Edit }
        private enMode _Mode;

        public int UserID { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public enUserRole Role { get; set; }
        public DateTime CreatedDate { get; set; }

        public clsUser()
        {
            this._Mode = enMode.AddNew;
            this.UserID = 0;
            this.Username = string.Empty;
            this.PasswordHash = string.Empty;
            this.Email = string.Empty;
            this.Phone = null;
            this.Role = enUserRole.User;
            this.CreatedDate = DateTime.Now;
        }

        private clsUser(int userid, string username, string passwordhash, string email, string? phone, enUserRole role, DateTime createddate)
        {
            this._Mode = enMode.Edit;
            this.UserID = userid;
            this.Username = username;
            this.PasswordHash = passwordhash;
            this.Email = email;
            this.Phone = phone;
            this.Role = role;
            this.CreatedDate = createddate;
        }

        private static clsUser _MapRowToUser(DataRow row)
        {
            int userid = Convert.ToInt32(row["UserID"]);
            string username = row["Username"] != DBNull.Value ? Convert.ToString(row["Username"])! : string.Empty;
            string passwordhash = row["PasswordHash"] != DBNull.Value ? Convert.ToString(row["PasswordHash"])! : string.Empty;
            string email = row["Email"] != DBNull.Value ? Convert.ToString(row["Email"])! : string.Empty;
            string? phone = row["Phone"] != DBNull.Value ? Convert.ToString(row["Phone"]) : null;
            enUserRole role = row["Role"] != DBNull.Value ? (enUserRole)Convert.ToInt32(row["Role"]) : enUserRole.User;
            DateTime createddate = row["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(row["CreatedDate"]) : DateTime.MinValue;

            return new clsUser(userid, username, passwordhash, email, phone, role, createddate);
        }

        public async Task<bool> Register(string password)
        {
            if (string.IsNullOrWhiteSpace(this.Username) || string.IsNullOrWhiteSpace(this.Email) || string.IsNullOrWhiteSpace(password))
                return false;

            DataTable dtByUsername = await clsUserData.GetUserByUsername(this.Username);
            if (dtByUsername.Rows.Count > 0)
                return false;

            DataTable dtByEmail = await clsUserData.GetUserByEmail(this.Email);
            if (dtByEmail.Rows.Count > 0)
                return false;

            this._Mode = enMode.AddNew;
            this.Role = enUserRole.User;
            this.PasswordHash = clsUtility.ComputeHash(password);

            int? NewID = await clsUserData.AddNewUser(this.Username, this.PasswordHash, this.Email, this.Phone, (int)this.Role);
            if (NewID.HasValue)
            {
                this.UserID = NewID.Value;
                this._Mode = enMode.Edit;
                return true;
            }
            else
                return false;
        }

        public static async Task<clsUser?> Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            DataTable dtRecords = await clsUserData.GetUserByUsername(username);
            if (dtRecords.Rows.Count > 0)
            {
                clsUser user = _MapRowToUser(dtRecords.Rows[0]);
                if (clsUtility.ComputeHash(password) == user.PasswordHash)
                    return user;
            }
            return null;
        }

        public static async Task<clsUser?> Find(int userID)
        {
            DataTable dtRecords = await clsUserData.GetUserByID(userID);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToUser(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<clsUser?> FindByUsername(string username)
        {
            DataTable dtRecords = await clsUserData.GetUserByUsername(username);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToUser(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<List<clsUser>> GetAll()
        {
            List<clsUser> lstUsers = new List<clsUser>();
            DataTable dtRecords = await clsUserData.GetAllUsers();
            foreach (DataRow row in dtRecords.Rows)
            {
                lstUsers.Add(_MapRowToUser(row));
            }
            return lstUsers;
        }

        public Task<bool> Update()
        {
            return clsUserData.UpdateUser(this.UserID, this.Username, this.Email, this.Phone, (int)this.Role);
        }

        public Task<bool> Delete()
        {
            return clsUserData.DeleteUser(this.UserID);
        }

        public async Task<bool> ChangePassword(string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;

            DataTable dtRecords = await clsUserData.GetUserByID(this.UserID);
            if (dtRecords.Rows.Count == 0)
                return false;

            clsUser currentUser = _MapRowToUser(dtRecords.Rows[0]);
            if (clsUtility.ComputeHash(currentPassword) != currentUser.PasswordHash)
                return false;

            string NewHash = clsUtility.ComputeHash(newPassword);
            bool IsChanged = await clsUserData.ChangePassword(this.UserID, NewHash);
            if (IsChanged)
                this.PasswordHash = NewHash;

            return IsChanged;
        }
    }
}
