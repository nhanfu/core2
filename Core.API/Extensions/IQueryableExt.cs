using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Models;

namespace Core.Extensions
{
    public static class IQueryableExt
    {
        public static async Task<int> BatchUpdateAsync<T>(this IQueryable<T> source, DbContext db, Action<T> action, int batch = 100)
        {
            var total = await source.CountAsync();
            var skip = 0;
            while (skip < total)
            {
                var list = await source.Skip(skip).Take(batch).ToListAsync();
                foreach (var item in list)
                {
                    action(item);
                }
                await db.SaveChangesAsync();
                skip += batch;
            }
            return total;
        }
    }
}
