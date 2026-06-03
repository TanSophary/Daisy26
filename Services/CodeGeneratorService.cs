using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;

namespace OnlineShop.Services
{
    /// <summary>
    /// Generates unique sequential codes for Category, Product, and Customer.
    ///
    /// CategoryCode : C01, C02, C03 …
    /// ProductCode  : I-C01-000001  (I = Item prefix, C01 = category code, 6-digit seq per category)
    /// CardCode     : C-26-00001    (C = Customer prefix, 26 = 2-digit year, 5-digit global seq)
    /// </summary>
    public class CodeGeneratorService
    {
        private readonly ApplicationDbContext _db;

        public CodeGeneratorService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ── Category ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next CategoryCode, e.g. C01, C02 …
        /// Thread-safe via DB MAX query.
        /// </summary>
        public async Task<string> NextCategoryCodeAsync()
        {
            // Find highest existing numeric suffix
            var existing = await _db.Categories
                .Where(c => c.CategoryCode != null && c.CategoryCode != "")
                .Select(c => c.CategoryCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in existing)
            {
                // Expect format "Cnn"
                if (code.StartsWith("C") && int.TryParse(code[1..], out int n))
                    max = Math.Max(max, n);
            }

            return $"C{(max + 1):D2}";
        }

        // ── Product ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next ProductCode for a given category, e.g. I-C01-000001
        /// </summary>
        public async Task<string> NextProductCodeAsync(string categoryCode)
        {
            var prefix = $"I-{categoryCode}-";

            var existing = await _db.Products
                .Where(p => p.ProductCode != null && p.ProductCode.StartsWith(prefix))
                .Select(p => p.ProductCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in existing)
            {
                var suffix = code[prefix.Length..];
                if (int.TryParse(suffix, out int n))
                    max = Math.Max(max, n);
            }

            return $"{prefix}{(max + 1):D6}";
        }

        // ── Customer ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the next CardCode, e.g. C-26-00001 (year is 2-digit current year)
        /// </summary>
        public async Task<string> NextCardCodeAsync()
        {
            var year = DateTime.Now.Year % 100; // 2026 → 26
            var yearPrefix = $"C-{year:D2}-";

            var existing = await _db.Customers
                .Where(c => c.CardCode != null && c.CardCode.StartsWith(yearPrefix))
                .Select(c => c.CardCode)
                .ToListAsync();

            int max = 0;
            foreach (var code in existing)
            {
                var suffix = code[yearPrefix.Length..];
                if (int.TryParse(suffix, out int n))
                    max = Math.Max(max, n);
            }

            return $"{yearPrefix}{(max + 1):D5}";
        }
    }
}
