using Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Core.ViewModels
{
    public class PatchUpdate
    {
        public List<PatchUpdateDetail> Changes { get; set; }

#if NETCOREAPP
        public void ApplyTo<T>(T obj)
        {
            if (obj == null)
            {
                return;
            }
            var idObj = obj.GetPropValue(Utils.IdField);
            if (idObj == null || idObj is not int)
            {
                return;
            }
            var isUpdate = (int)idObj > 0;
            foreach (var prop in obj.GetType().GetProperties().Where(x => x.CanWrite && x.PropertyType.IsSimple()))
            {
                var fieldPatch = Changes.FirstOrDefault(x => x.Field == prop.Name);
                if (fieldPatch is null)
                {
                    continue;
                }
                var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                var parsedVal = converter.ConvertFromInvariantString(fieldPatch.Value);
                if (isUpdate)
                {
                    //CheckConflict(obj, prop, fieldPatch);
                }
                prop.SetValue(obj, parsedVal);
            }
        }

        private static void CheckConflict<T>(T obj, System.Reflection.PropertyInfo prop, PatchUpdateDetail fieldPatch)
        {
            if (prop.PropertyType.IsDate())
            {
                var dbVal = ((DateTime?)prop.GetValue(obj, null))?.ToString("yyyy/MM/dd HH:mm:ss");
                if (dbVal != fieldPatch.OldVal && fieldPatch.Field != Utils.IdField)
                {
                    throw new Exceptions.ApiException($"Dữ liệu đã thanh đổi trước đó .\n" +
                        $"Từ {dbVal} => {fieldPatch.OldVal}")
                    {
                        StatusCode = Enums.HttpStatusCode.BadRequest
                    };
                }
            }
            else if (prop.PropertyType.IsDecimal())
            {
                var dbVal = ((decimal?)prop.GetValue(obj, null))?.ToString("N5");
                if (dbVal != decimal.Parse(fieldPatch.OldVal).ToString("N5") && fieldPatch.Field != Utils.IdField)
                {
                    throw new Exceptions.ApiException($"Dữ liệu đã thanh đổi trước đó .\n" +
                        $"Từ {dbVal} => {decimal.Parse(fieldPatch.OldVal).ToString("N5")}")
                    {
                        StatusCode = Enums.HttpStatusCode.BadRequest
                    };
                }
            }
            else
            {
                var dbVal = prop.GetValue(obj, null)?.ToString();
                if (dbVal != fieldPatch.OldVal && fieldPatch.Field != Utils.IdField)
                {
                    throw new Exceptions.ApiException($"Dữ liệu đã thanh đổi trước đó .\n" +
                        $"Từ {dbVal} => {fieldPatch.OldVal}")
                    {
                        StatusCode = Enums.HttpStatusCode.BadRequest
                    };
                }
            }
        }
#endif
    }

    public class PatchUpdateDetail
    {
        public string Field { get; set; }
        public string OldVal { get; set; }
        public string Value { get; set; }
    }
}
