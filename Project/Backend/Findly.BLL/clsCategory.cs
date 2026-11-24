using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using LostFound.DAL;

namespace LostFound.BLL
{
    public partial class clsCategory
    {
        private enum enMode { AddNew, Edit }
        private enMode _Mode;

        public int CategoryID { get; set; }
        public string Name { get; set; }

        public clsCategory()
        {
            this._Mode = enMode.AddNew;
            this.CategoryID = 0;
            this.Name = string.Empty;
        }

        private clsCategory(int categoryid, string name)
        {
            this._Mode = enMode.Edit;
            this.CategoryID = categoryid;
            this.Name = name;
        }

        private static clsCategory _MapRowToCategory(DataRow row)
        {
            int categoryid = Convert.ToInt32(row["CategoryID"]);
            string name = row["Name"] != DBNull.Value ? Convert.ToString(row["Name"])! : string.Empty;
            return new clsCategory(categoryid, name);
        }

        private async Task<bool> _AddNew()
        {
            int? NewID = await clsCategoryData.AddNewCategory(this.Name);
            if (NewID.HasValue)
            {
                this.CategoryID = NewID.Value;
                return true;
            }
            else
                return false;
        }

        private Task<bool> _Edit()
        {
            return clsCategoryData.UpdateCategory(this.CategoryID, this.Name);
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

        public static async Task<clsCategory?> Find(int categoryID)
        {
            DataTable dtRecords = await clsCategoryData.GetCategoryByID(categoryID);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToCategory(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<List<clsCategory>> GetAll()
        {
            List<clsCategory> lstCategories = new List<clsCategory>();
            DataTable dtRecords = await clsCategoryData.GetAllCategories();
            foreach (DataRow row in dtRecords.Rows)
            {
                lstCategories.Add(_MapRowToCategory(row));
            }
            return lstCategories;
        }

        public Task<bool> Delete()
        {
            return clsCategoryData.DeleteCategory(this.CategoryID);
        }
    }
}
