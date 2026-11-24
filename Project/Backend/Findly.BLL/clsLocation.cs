using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using LostFound.DAL;

namespace LostFound.BLL
{
    public partial class clsLocation
    {
        private enum enMode { AddNew, Edit }
        private enMode _Mode;

        public int LocationID { get; set; }
        public string Name { get; set; }

        public clsLocation()
        {
            this._Mode = enMode.AddNew;
            this.LocationID = 0;
            this.Name = string.Empty;
        }

        private clsLocation(int locationid, string name)
        {
            this._Mode = enMode.Edit;
            this.LocationID = locationid;
            this.Name = name;
        }

        private static clsLocation _MapRowToLocation(DataRow row)
        {
            int locationid = Convert.ToInt32(row["LocationID"]);
            string name = row["Name"] != DBNull.Value ? Convert.ToString(row["Name"])! : string.Empty;
            return new clsLocation(locationid, name);
        }

        private async Task<bool> _AddNew()
        {
            int? NewID = await clsLocationData.AddNewLocation(this.Name);
            if (NewID.HasValue)
            {
                this.LocationID = NewID.Value;
                return true;
            }
            else
                return false;
        }

        private Task<bool> _Edit()
        {
            return clsLocationData.UpdateLocation(this.LocationID, this.Name);
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

        public static async Task<clsLocation?> Find(int locationID)
        {
            DataTable dtRecords = await clsLocationData.GetLocationByID(locationID);
            if (dtRecords.Rows.Count > 0)
                return _MapRowToLocation(dtRecords.Rows[0]);
            else
                return null;
        }

        public static async Task<List<clsLocation>> GetAll()
        {
            List<clsLocation> lstLocations = new List<clsLocation>();
            DataTable dtRecords = await clsLocationData.GetAllLocations();
            foreach (DataRow row in dtRecords.Rows)
            {
                lstLocations.Add(_MapRowToLocation(row));
            }
            return lstLocations;
        }

        public Task<bool> Delete()
        {
            return clsLocationData.DeleteLocation(this.LocationID);
        }
    }
}
