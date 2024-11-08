using Core.Extensions;
using Core.Models;
using Core.Services;
using CoreAPI.Models;
using LinqKit;
using Microsoft.Build.Evaluation;

namespace CoreAPI.BgService
{
    public class CustomerFunction
    {
        IConfiguration _config;
        public CustomerFunction(IConfiguration configuration)
        {
            _config = configuration;
        }

        public async Task StatisticsProcesses()
        {
            var connect = _config.GetConnectionString("Default");
            await LockDeleteAsync(connect);
        }

        private async Task LockDeleteAsync(string connect)
        {
            var lockProspect = await BgExt.ReadDsAsArr<SaleFunction>(
                $@"SELECT * FROM MASTERDATA WHERE CODE = N'DAY_LOCK_PROSPECT'", connect);
            var loclLeads = await BgExt.ReadDsAsArr<SaleFunction>(
                $@"SELECT * FROM MASTERDATA WHERE CODE = N'DAY_LOCK_LEADS'", connect);
            if (lockProspect != null && lockProspect[0] != null && lockProspect[0].Value != null && int.Parse(lockProspect[0].Value) > 0)
            {
                var partnerCare = await BgExt.ReadDsAsArr<Partner>(
                $@"SELECT 
                Partner.*
                FROM 
                    Partner
                OUTER APPLY (
                    SELECT TOP 1 * 
                    FROM PartnerCare 
                    WHERE Partner.Id = PartnerCare.PartnerId 
                    ORDER BY InsertedDate DESC, UpdatedDate DESC
                ) AS care
                WHERE 
                    ServiceId = 1
                    AND TypeId = 2
                    AND Partner.ActionId = 1
                    AND DATEDIFF(DAY, ISNULL(ISNULL(care.UpdatedDate, care.InsertedDate), Partner.InsertedDate), GETDATE()) > {lockProspect[0].Value};", connect);
                if (partnerCare != null && partnerCare.Length > 0)
                {
                    foreach (var item in partnerCare)
                    {
                        item.ActionId = 2;
                        var patch = item.MapToPatch();
                        await BgExt.SavePatch2(patch, connect);
                    }
                }
            }
            if (loclLeads != null && loclLeads[0] != null && loclLeads[0].Value != null && int.Parse(lockProspect[0].Value) > 0)
            {
                var partnerCare = await BgExt.ReadDsAsArr<Partner>(
                $@"SELECT 
                Partner.*
                FROM 
                    Partner
                WHERE 
                    ServiceId = 1
                    AND TypeId = 1
                    AND Partner.ActionId = 1
                    AND DATEDIFF(DAY, Partner.InsertedDate, GETDATE()) > {loclLeads[0].Value};", connect);
                if (partnerCare != null && partnerCare.Length > 0)
                {
                    foreach (var item in partnerCare)
                    {
                        item.ActionId = 2;
                        var patch = item.MapToPatch();
                        await BgExt.SavePatch2(patch, connect);
                    }
                }
            }
        }
    }
}
